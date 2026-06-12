namespace ecommerce.Services.Payments
{
    public class RefundResult
    {
        public string? ProviderRefundReference { get; set; }

        public bool RequiresManualSettlement { get; set; }
    }
}
