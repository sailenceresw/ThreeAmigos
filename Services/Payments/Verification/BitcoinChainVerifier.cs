using System.Text.Json;
using ecommerce.Settings;
using Microsoft.Extensions.Options;

namespace ecommerce.Services.Payments.Verification
{
    public class BitcoinChainVerifier : IChainVerifier
    {
        private readonly HttpClient _http;
        private readonly ILogger<BitcoinChainVerifier> _logger;
        private readonly CryptoVerificationOptions _options;

        public BitcoinChainVerifier(HttpClient http, IOptions<PaymentOptions> options, ILogger<BitcoinChainVerifier> logger)
        {
            _http = http;
            _logger = logger;
            _options = options.Value.Crypto.Verification;
        }

        public string Currency => "BTC";

        public async Task<ChainVerificationResult> VerifyAsync(
            string txHash,
            string expectedAddress,
            decimal? expectedAmount,
            int minConfirmations,
            decimal amountTolerance,
            CancellationToken ct)
        {
            var result = new ChainVerificationResult();
            var baseUrl = _options.MempoolSpaceBaseUrl.TrimEnd('/');

            try
            {
                using var txResp = await _http.GetAsync($"{baseUrl}/api/tx/{txHash}", ct);
                if (txResp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Outcome = ChainVerificationOutcome.TxNotFound;
                    result.Reason = "Transaction not seen on Bitcoin network yet.";
                    return result;
                }
                txResp.EnsureSuccessStatusCode();
                using var txStream = await txResp.Content.ReadAsStreamAsync(ct);
                using var txDoc = await JsonDocument.ParseAsync(txStream, cancellationToken: ct);

                long satoshisToUs = 0;
                if (txDoc.RootElement.TryGetProperty("vout", out var vouts) && vouts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var vout in vouts.EnumerateArray())
                    {
                        if (vout.TryGetProperty("scriptpubkey_address", out var addr)
                            && string.Equals(addr.GetString(), expectedAddress, StringComparison.OrdinalIgnoreCase)
                            && vout.TryGetProperty("value", out var value))
                        {
                            satoshisToUs += value.GetInt64();
                        }
                    }
                }

                result.RecipientMatched = satoshisToUs > 0;
                result.ReceivedAmount = satoshisToUs / 100_000_000m;

                if (!result.RecipientMatched)
                {
                    result.Outcome = ChainVerificationOutcome.Rejected;
                    result.Reason = $"Transaction does not pay {expectedAddress}.";
                    return result;
                }

                if (expectedAmount.HasValue && expectedAmount.Value > 0)
                {
                    var delta = Math.Abs(result.ReceivedAmount.Value - expectedAmount.Value) / expectedAmount.Value;
                    result.AmountMatched = delta <= amountTolerance;
                    if (!result.AmountMatched)
                    {
                        result.Outcome = ChainVerificationOutcome.Rejected;
                        result.Reason = $"Received {result.ReceivedAmount} BTC, expected {expectedAmount} (>{amountTolerance:P0} drift).";
                        return result;
                    }
                }
                else
                {
                    result.AmountMatched = true;
                }

                if (!txDoc.RootElement.TryGetProperty("status", out var statusElem)
                    || !statusElem.TryGetProperty("confirmed", out var confirmed)
                    || !confirmed.GetBoolean())
                {
                    result.Outcome = ChainVerificationOutcome.Pending;
                    result.Confirmations = 0;
                    result.Reason = "Transaction in mempool, not yet confirmed.";
                    return result;
                }

                var blockHeight = statusElem.GetProperty("block_height").GetInt32();

                using var tipResp = await _http.GetAsync($"{baseUrl}/api/blocks/tip/height", ct);
                tipResp.EnsureSuccessStatusCode();
                var tipText = await tipResp.Content.ReadAsStringAsync(ct);
                var tipHeight = int.Parse(tipText.Trim());

                result.Confirmations = Math.Max(0, tipHeight - blockHeight + 1);
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
                _logger.LogWarning(ex, "Bitcoin verification failed for tx {TxHash}", txHash);
                result.Outcome = ChainVerificationOutcome.Unknown;
                result.Reason = "Verification call failed; will retry.";
                return result;
            }
        }
    }
}
