using CurrX.Infrastructure;
using CurrX.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrX.Services
{
    internal class RateDatabaseService(string databasePath)
    {
        private readonly string _databasePath = databasePath;

        private AppDbContext CreateContext()
        {
            return new AppDbContext(_databasePath);
        }

        public async Task InitializeAsync()
        {
            await using AppDbContext db = CreateContext();
            await db.Database.EnsureCreatedAsync();
        }

        public async Task<bool> ExistsAsync(string code, string exchangeDate)
        {
            await using AppDbContext db = CreateContext();
            return await db.Rates.AnyAsync(e =>
                e.Code == code && e.ExchangeDate == exchangeDate);
        }

        public async Task<int> AddAsync(SavedRateEntry item)
        {
            await using AppDbContext db = CreateContext();
            db.Rates.Add(item);
            await db.SaveChangesAsync();
            return item.Id;
        }

        public async Task<List<SavedRateEntry>> GetAllAsync()
        {
            await using AppDbContext db = CreateContext();
            return await db.Rates
            .AsNoTracking()
            .OrderByDescending(item => item.SavedAtUtc)
            .ToListAsync();
        }

        public async Task<bool> UpdateAsync(
            int id, string notes)
        {
            await using AppDbContext db = CreateContext();
            SavedRateEntry? item = await db.Rates.FindAsync(id);
            if (item is null)
            {
                return false;
            }
            item.Notes = notes.Trim();
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using AppDbContext db = CreateContext();
            SavedRateEntry? item = await db.Rates.FindAsync(id);
            if (item is null)
            {
                return false;
            }
            db.Rates.Remove(item);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
