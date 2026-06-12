using ecommerce.Models;
using ecommerce.Services.Payments.Verification;
using ecommerce.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ecommerce.Services.Payments
{
    public class CryptoVerificationHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CryptoVerificationHostedService> _logger;
        private readonly CryptoVerificationOptions _options;

        public CryptoVerificationHostedService(
            IServiceProvider serviceProvider,
            IOptions<PaymentOptions> options,
            ILogger<CryptoVerificationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value.Crypto.Verification;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.AutoVerifyEnabled)
            {
                _logger.LogInformation("Crypto auto-verification disabled; hosted service idle.");
                return;
            }

            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.PollIntervalMinutes));
            _logger.LogInformation("Crypto auto-verification poll interval: {Interval}", interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunSweepAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Crypto verification sweep threw.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (TaskCanceledException) { }
            }
        }

        private async Task RunSweepAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Context>();
            var resolver = scope.ServiceProvider.GetRequiredService<IChainVerifierResolver>();
            var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            var pending = await context.Payment
                .Where(p => p.Method == PaymentMethod.CryptoManual
                            && p.Status == PaymentStatus.AwaitingConfirmation
                            && p.CryptoTxHash != null
                            && p.VerificationAttempts < _options.MaxAttempts)
                .OrderBy(p => p.LastVerifiedAt ?? p.CreatedAt)
                .Take(25)
                .ToListAsync(ct);

            foreach (var payment in pending)
            {
                if (ct.IsCancellationRequested) break;

                var verifier = string.IsNullOrWhiteSpace(payment.CryptoCurrency)
                    ? null
                    : resolver.Resolve(payment.CryptoCurrency);
                if (verifier == null)
                {
                    payment.LastVerifiedAt = DateTime.UtcNow;
                    payment.VerificationAttempts += 1;
                    context.Payment.Update(payment);
                    await context.SaveChangesAsync(ct);
                    continue;
                }

                var minConf = string.Equals(payment.CryptoCurrency, "BTC", StringComparison.OrdinalIgnoreCase)
                    ? _options.MinConfirmationsBitcoin
                    : _options.MinConfirmationsEthereum;

                var verification = await verifier.VerifyAsync(
                    payment.CryptoTxHash!,
                    payment.CryptoWalletAddress ?? "",
                    payment.CryptoExpectedAmount,
                    minConf,
                    _options.AmountTolerance,
                    ct);

                payment.LastVerifiedAt = DateTime.UtcNow;
                payment.VerificationAttempts += 1;
                payment.CryptoReceivedAmount = verification.ReceivedAmount;
                payment.CryptoConfirmations = verification.Confirmations;
                context.Payment.Update(payment);
                await context.SaveChangesAsync(ct);

                switch (verification.Outcome)
                {
                    case ChainVerificationOutcome.Confirmed:
                        _logger.LogInformation("Auto-confirming payment {PaymentId} ({Currency} tx {Tx})",
                            payment.Id, payment.CryptoCurrency, payment.CryptoTxHash);
                        await payments.MarkSucceededAsync(payment.Id, confirmedByUserId: null, ct);
                        break;
                    case ChainVerificationOutcome.Rejected:
                        _logger.LogWarning("Auto-rejecting payment {PaymentId}: {Reason}", payment.Id, verification.Reason);
                        await payments.MarkFailedAsync(payment.Id, verification.Reason ?? "Verification failed", ct);
                        break;
                    case ChainVerificationOutcome.Pending:
                    case ChainVerificationOutcome.TxNotFound:
                    case ChainVerificationOutcome.Unknown:
                    default:
                        break;
                }
            }
        }
    }
}
