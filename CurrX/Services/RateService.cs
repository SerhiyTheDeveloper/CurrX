using CurrX.Entities;
using System.Net.Http;
using System.Text.Json;

namespace CurrX.Services
{
    internal static class RateService
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };

        public static async Task<RateEntry> LoadRateAsync(string url)
        {
            string json = await Client.GetStringAsync(url);
            
            using JsonDocument document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;
            JsonElement item = root[0];

            string currency = item.GetProperty("txt").GetString() ?? string.Empty;
            decimal rate = item.GetProperty("rate").GetDecimal();
            string code = item.GetProperty("cc").GetString() ?? string.Empty;
            string exchangeDate = item.GetProperty("exchangedate").GetString() ?? string.Empty;

            return new RateEntry
            {
                Currency = currency,
                Rate = rate,
                Code = code,
                ExchangeDate = exchangeDate
            };
        }
    }
}
