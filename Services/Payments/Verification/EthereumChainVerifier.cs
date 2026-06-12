using System.Globalization;
using System.Numerics;
using System.Text.Json;
using ecommerce.Settings;
using Microsoft.Extensions.Options;

namespace ecommerce.Services.Payments.Verification
{
    public class EthereumChainVerifier : IChainVerifier
    {
        private readonly HttpClient _http;
        private readonly ILogger<EthereumChainVerifier> _logger;
        private readonly CryptoVerificationOptions _options;

        public EthereumChainVerifier(HttpClient http, IOptions<PaymentOptions> options, ILogger<EthereumChainVerifier> logger)
        {
            _http = http;
            _logger = logger;
            _options = options.Value.Crypto.Verification;
        }

        public string Currency => "ETH";

        public async Task<ChainVerificationResult> VerifyAsync(
            string txHash,
            string expectedAddress,
            decimal? expectedAmount,
            int minConfirmations,
            decimal amountTolerance,
            CancellationToken ct)
        {
            var result = new ChainVerificationResult();
            if (string.IsNullOrWhiteSpace(_options.EtherscanApiKey))
            {
                result.Outcome = ChainVerificationOutcome.Unknown;
                result.Reason = "EtherscanApiKey is not configured; cannot verify Ethereum transactions.";
                return result;
            }

            var baseUrl = _options.EtherscanBaseUrl.TrimEnd('/');
            var key = _options.EtherscanApiKey;

            try
            {
                var txJson = await GetJsonAsync($"{baseUrl}/api?module=proxy&action=eth_getTransactionByHash&txhash={txHash}&apikey={key}", ct);
                if (!txJson.RootElement.TryGetProperty("result", out var txRes) || txRes.ValueKind != JsonValueKind.Object)
                {
                    result.Outcome = ChainVerificationOutcome.TxNotFound;
                    result.Reason = "Transaction not seen on Ethereum network yet.";
                    return result;
                }

                var toAddr = txRes.TryGetProperty("to", out var toElem) ? toElem.GetString() : null;
                var valueHex = txRes.TryGetProperty("value", out var valElem) ? valElem.GetString() : null;
                var blockNumberHex = txRes.TryGetProperty("blockNumber", out var bnElem) ? bnElem.GetString() : null;

                if (string.IsNullOrEmpty(toAddr) || string.IsNullOrEmpty(valueHex))
                {
                    result.Outcome = ChainVerificationOutcome.Rejected;
                    result.Reason = "Transaction payload missing required fields.";
                    return result;
                }

                result.RecipientMatched = string.Equals(toAddr, expectedAddress, StringComparison.OrdinalIgnoreCase);
                if (!result.RecipientMatched)
                {
                    result.Outcome = ChainVerificationOutcome.Rejected;
                    result.Reason = $"Transaction recipient {toAddr} does not match expected {expectedAddress}.";
                    return result;
                }

                var weiValue = HexToBigInteger(valueHex);
                result.ReceivedAmount = (decimal)((double)weiValue / 1e18);

                if (expectedAmount.HasValue && expectedAmount.Value > 0)
                {
                    var delta = Math.Abs(result.ReceivedAmount.Value - expectedAmount.Value) / expectedAmount.Value;
                    result.AmountMatched = delta <= amountTolerance;
                    if (!result.AmountMatched)
                    {
                        result.Outcome = ChainVerificationOutcome.Rejected;
                        result.Reason = $"Received {result.ReceivedAmount} ETH, expected {expectedAmount} (>{amountTolerance:P0} drift).";
                        return result;
                    }
                }
                else
                {
                    result.AmountMatched = true;
                }

                if (string.IsNullOrEmpty(blockNumberHex))
                {
                    result.Outcome = ChainVerificationOutcome.Pending;
                    result.Confirmations = 0;
                    result.Reason = "Transaction not yet included in a block.";
                    return result;
                }

                var receiptJson = await GetJsonAsync($"{baseUrl}/api?module=proxy&action=eth_getTransactionReceipt&txhash={txHash}&apikey={key}", ct);
                if (receiptJson.RootElement.TryGetProperty("result", out var receipt)
                    && receipt.ValueKind == JsonValueKind.Object
                    && receipt.TryGetProperty("status", out var statusEl)
                    && statusEl.GetString() == "0x0")
                {
                    result.Outcome = ChainVerificationOutcome.Rejected;
                    result.Reason = "Transaction reverted on-chain.";
                    return result;
                }

                var blockNumber = (int)HexToBigInteger(blockNumberHex);

                var tipJson = await GetJsonAsync($"{baseUrl}/api?module=proxy&action=eth_blockNumber&apikey={key}", ct);
                var tipHex = tipJson.RootElement.GetProperty("result").GetString() ?? "0x0";
                var tipBlock = (int)HexToBigInteger(tipHex);

                result.Confirmations = Math.Max(0, tipBlock - blockNumber + 1);
                result.Outcome = result.Confirmations >= minConfirmations
                    ? ChainVerificationOutcome.Confirmed
                    : ChainVerificationOutcome.Pending;
                if (result.Outcome == ChainVerificationOutcome.Pending)
                {
                    result.Reason = $"Awaiting confirmations ({result.Confirmations}/{minConfirmations}).";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ethereum verification failed for tx {TxHash}", txHash);
                result.Outcome = ChainVerificationOutcome.Unknown;
                result.Reason = "Verification call failed; will retry.";
                return result;
            }
        }

        private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
        {
            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }

        private static BigInteger HexToBigInteger(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }
            if (hex.Length == 0) return BigInteger.Zero;
            if (hex.Length % 2 == 1) hex = "0" + hex;
            return BigInteger.Parse("0" + hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
    }
}
