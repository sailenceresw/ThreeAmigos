namespace ecommerce.Settings
{
    public class StockOptions
    {
        public const string SectionName = "Stock";

        public int ReservationTtlMinutes { get; set; } = 15;
        public int SweepIntervalMinutes { get; set; } = 5;
    }
}
