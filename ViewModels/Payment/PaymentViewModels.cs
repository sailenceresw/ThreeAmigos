using System.ComponentModel.DataAnnotations;
using ecommerce.Models;
using ecommerce.Settings;

namespace ecommerce.ViewModels.Payment
{
    public class SelectPaymentMethodViewModel
    {
        public int OrderId { get; set; }
        public decimal AmountUsd { get; set; }

        public bool InAppBalanceEnabled { get; set; }
        public decimal UserBalance { get; set; }
        public bool StripeEnabled { get; set; }
        public bool CryptoEnabled { get; set; }
        public List<CryptoWalletOption> CryptoWallets { get; set; } = new();
    }

    public class StripeCheckoutViewModel
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public decimal AmountUsd { get; set; }
        public string ClientSecret { get; set; } = "";
        public string PublishableKey { get; set; } = "";
    }

    public class CryptoCheckoutViewModel
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public decimal AmountUsd { get; set; }
        public string Currency { get; set; } = "";
        public string Network { get; set; } = "";
        public string WalletAddress { get; set; } = "";

        [Required, MinLength(8), MaxLength(256)]
        public string? TxHash { get; set; }
    }

    public class CryptoTxSubmissionViewModel
    {
        [Required]
        public int PaymentId { get; set; }

        [Required, MinLength(8), MaxLength(256)]
        public string TxHash { get; set; } = "";
    }

    public class AdminCryptoReviewViewModel
    {
        public List<Models.Payment> Pending { get; set; } = new();
    }

    public class AdminPaymentsViewModel
    {
        public List<Models.Payment> Recent { get; set; } = new();
    }
}
