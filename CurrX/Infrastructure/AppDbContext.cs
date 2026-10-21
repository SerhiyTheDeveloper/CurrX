using CurrX.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrX.Infrastructure
{
    internal class AppDbContext(string databasePath) : DbContext
    {
        private readonly string _databasePath = databasePath;
        public DbSet<SavedRateEntry> Rates => Set<SavedRateEntry>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SavedRateEntry>()
                .Property(item => item.Code)
                .HasMaxLength(10)
                .IsRequired();
            modelBuilder.Entity<SavedRateEntry>()
                .Property(item => item.Currency)
                .IsRequired();
            modelBuilder.Entity<SavedRateEntry>()
                .Property(item => item.Notes)
                .HasMaxLength(500);
            modelBuilder.Entity<SavedRateEntry>()
                .HasIndex(e => new { e.Code, e.ExchangeDate })
                .IsUnique();
        }
    }
}
