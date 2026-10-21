namespace CurrX.Models
{
    internal class RateEntrySnapshot
    {
        public string DataSource { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public string FormatVersion { get; set; } = string.Empty;
        public DateTimeOffset SavedAt { get; set; }
        public RateEntry? RateEntry { get; set; }
    }
}
