namespace CurrX.Entities
{
    internal class RateEntry
    {
        public string Currency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ExchangeDate { get; set; } = string.Empty;
    }
}
