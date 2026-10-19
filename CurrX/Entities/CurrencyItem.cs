namespace CurrX.Entities
{
    internal class CurrencyItem
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Code} · {Name}";
        }
    }
}
