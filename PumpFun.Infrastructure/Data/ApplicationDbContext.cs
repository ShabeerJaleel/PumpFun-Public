using Microsoft.EntityFrameworkCore;
using PumpFun.Core.Models;

namespace PumpFun.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Token> Tokens { get; set; } = null!;
        public DbSet<Trade> Trades { get; set; } = null!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Token>(entity =>
            {
                // Primary key
                entity.HasKey(t => t.TokenAddress);

                // Individual indexes for frequently queried fields
                entity.HasIndex(t => t.MarketCapSol)
                     .HasDatabaseName("IX_Token_MarketCap");
                     
                entity.HasIndex(t => t.CreatedAt)
                     .HasDatabaseName("IX_Token_CreatedAt");
                     
                entity.HasIndex(t => t.AnalysisCompleted)
                     .HasDatabaseName("IX_Token_AnalysisCompleted");

                // Add composite indexes
                entity.HasIndex(t => new { t.MarketCapSol, t.CreatedAt })
                     .HasDatabaseName("IX_Token_MarketCap_CreatedAt");

                entity.HasIndex(t => new { t.AnalysisCompleted, t.CreatedAt })
                     .HasDatabaseName("IX_Token_Analysis_CreatedAt");

                // Precision configurations
                entity.Property(e => e.VSol).HasPrecision(18, 4);
                entity.Property(e => e.VTokens).HasPrecision(18, 4);
                entity.Property(e => e.MarketCapSol).HasPrecision(18, 4);
                entity.Property(e => e.InitialBuy).HasPrecision(18, 4);
            });

            modelBuilder.Entity<Trade>(entity =>
            {
                entity.HasKey(e => e.Signature);
                entity.Property(e => e.Signature).HasMaxLength(100);  // Adjust length as needed

                // Specify precision for decimal properties
                entity.Property(e => e.MarketCapSol)
                      .HasPrecision(18, 4);
                entity.Property(e => e.NewTokenBalance)
                      .HasPrecision(18, 4);
                entity.Property(e => e.TokenAmount)
                      .HasPrecision(18, 4);
                entity.Property(e => e.VSolInBondingCurve)
                      .HasPrecision(18, 4);
                entity.Property(e => e.VTokensInBondingCurve)
                      .HasPrecision(18, 4);
            });
        }
    }
}