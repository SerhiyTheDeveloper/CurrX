namespace CurrX.Models
{
    internal class SavedRateEntry
    {
        public int Id { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ExchangeDate { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }
}
