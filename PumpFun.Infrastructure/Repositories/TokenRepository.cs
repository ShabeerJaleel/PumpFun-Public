using System.Threading.Tasks;
using PumpFun.Core.Models;
using PumpFun.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models.Requests;
using System.Diagnostics; // Add this at the top with other using statements

namespace PumpFun.Infrastructure.Repositories
{
    public class TokenRepository : ITokenRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<TokenRepository> _logger;

        public TokenRepository(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<TokenRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task AddOrUpdateTokenAsync(Token token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            try 
            {
                await context.Tokens.AddAsync(token);  // Try add first
                await context.SaveChangesAsync();
                _logger.LogInformation("Added new token: {Symbol}, {TokenAddress}", 
                    token.Symbol, token.TokenAddress);
            }
            catch (DbUpdateException) // If primary key violation
            {
                context.ChangeTracker.Clear(); // Clear failed insert
                context.Tokens.Update(token);  // Try update
                await context.SaveChangesAsync();
            }
        }

        // Read-only operations - keep AsNoTracking
        public async Task<IEnumerable<Token>> GetAllTokensAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tokens
                .AsNoTracking()  // Add this since we're just reading
                .ToListAsync();
        }

        // Keep tracking enabled - this token will be modified
        public async Task<Token?> GetTokenAsync(string tokenAddress)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tokens.FindAsync(tokenAddress);
        }

        // Read-only operations - keep AsNoTracking
        public async Task<IEnumerable<Token>> GetTokensAsync(TokenFilterRequest filter)
        {
            var sw = Stopwatch.StartNew();
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Base query with NOLOCK hint
            IQueryable<Token> query = context.Tokens
                .FromSqlRaw("SELECT * FROM Tokens WITH (NOLOCK)").AsNoTracking();

            // Apply filters
            if (filter.MarketCapMinSol.HasValue)
                query = query.Where(t => t.MarketCapSol >= filter.MarketCapMinSol.Value);
            
            if (filter.MarketCapMaxSol.HasValue)
                query = query.Where(t => t.MarketCapSol <= filter.MarketCapMaxSol.Value);

            if (filter.AnalysisCompleted.HasValue)
                query = query.Where(t => t.AnalysisCompleted == filter.AnalysisCompleted.Value);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(t => t.CreatedAt >= filter.CreatedAfter.Value);

            query = query.OrderByDescending(t => t.CreatedAt);

            if (filter.PageNumber.HasValue && filter.PageSize.HasValue)
            {
                query = query.Skip((filter.PageNumber.Value - 1) * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }
            else
            {
                query = query.Take(filter.Limit);
            }

            var result = await query.ToListAsync();
            sw.Stop();

            _logger.LogDebug(
                "GetTokensAsync completed in {ElapsedMs}ms. Filters: MarketCap({MinSol}-{MaxSol}), AnalysisCompleted:{Analysis}, CreatedAfter:{Created}, Page:{Page}, Size:{Size}, Limit:{Limit}, Results:{Count}",
                sw.ElapsedMilliseconds,
                filter.MarketCapMinSol,
                filter.MarketCapMaxSol,
                filter.AnalysisCompleted,
                filter.CreatedAfter,
                filter.PageNumber,
                filter.PageSize,
                filter.Limit,
                result.Count);

            return result;
        }

        public async Task<Token> UpdateTokenMarketCapAsync(string tokenAddress, decimal marketCapSol)
        {
            for (int i = 0; i < 3; i++) // Try up to 3 times (initial + 2 retries)
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var token = await context.Tokens.FindAsync(tokenAddress);
                
                if (token != null)
                {
                    token.MarketCapSol = marketCapSol;
                    await context.SaveChangesAsync();
                    return token;
                }

                await Task.Delay(1000); // Wait 1 second between retries
            }

            return null;
        }

        public async Task UpdateTokenAnalysisAsync(string tokenAddress, string analysis)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var token = await context.Tokens.FindAsync(tokenAddress);
            token.Analysis = analysis;
            token.AnalysisCompleted = true;
            token.AnalysisTimestamp = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task UpdateTokenAsync(Token token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Tokens.Update(token);
            await context.SaveChangesAsync();
        }
    }
}