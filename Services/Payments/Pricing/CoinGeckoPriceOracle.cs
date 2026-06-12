using System.Text.Json;
using ecommerce.Settings;
using Microsoft.Extensions.Options;

namespace ecommerce.Services.Payments.Pricing
{
    public class CoinGeckoPriceOracle : IPriceOracle
    {
        private readonly HttpClient _http;
        private readonly ILogger<CoinGeckoPriceOracle> _logger;
        private readonly CryptoVerificationOptions _options;

        private static readonly Dictionary<string, string> CurrencyToId = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BTC"] = "bitcoin",
            ["ETH"] = "ethereum",
            ["USDT"] = "tether",
            ["USDC"] = "usd-coin",
        };

        public CoinGeckoPriceOracle(HttpClient http, IOptions<PaymentOptions> options, ILogger<CoinGeckoPriceOracle> logger)
        {
            _http = http;
            _logger = logger;
            _options = options.Value.Crypto.Verification;
        }

        public async Task<decimal?> GetUsdPriceAsync(string currency, CancellationToken ct)
        {
            if (!CurrencyToId.TryGetValue(currency, out var id))
            {
                return null;
            }

            try
            {
                var url = $"{_options.CoinGeckoBaseUrl.TrimEnd('/')}/api/v3/simple/price?ids={id}&vs_currencies=usd";
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CoinGecko price lookup for {Currency} returned {Status}", currency, resp.StatusCode);
                    return null;
                }
                using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                if (!doc.RootElement.TryGetProperty(id, out var coinElem)) return null;
                if (!coinElem.TryGetProperty("usd", out var usdElem)) return null;
                return usdElem.GetDecimal();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CoinGecko price lookup for {Currency} threw", currency);
                return null;
            }
        }
    }
}
