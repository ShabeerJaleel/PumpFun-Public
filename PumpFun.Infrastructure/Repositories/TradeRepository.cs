using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models;
using PumpFun.Infrastructure.Data;

namespace PumpFun.Infrastructure.Repositories
{
    public class TradeRepository : ITradeRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<TradeRepository> _logger;

        public TradeRepository(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<TradeRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task AddTradeAsync(Trade trade)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existingTrade = await context.Trades
                .FirstOrDefaultAsync(t => t.Signature == trade.Signature);

            if (existingTrade == null)
            {
                await context.Trades.AddAsync(trade);
                _logger.LogInformation("Added new trade: {Signature}", trade.Signature);
            }
            else
            {
                existingTrade.TokenAddress = trade.TokenAddress;
                existingTrade.TraderPublicKey = trade.TraderPublicKey;
                existingTrade.TxType = trade.TxType;
                existingTrade.TokenAmount = trade.TokenAmount;
                existingTrade.NewTokenBalance = trade.NewTokenBalance;
                existingTrade.VTokensInBondingCurve = trade.VTokensInBondingCurve;
                existingTrade.VSolInBondingCurve = trade.VSolInBondingCurve;
                existingTrade.MarketCapSol = trade.MarketCapSol;
                context.Trades.Update(existingTrade);
                _logger.LogInformation("Updated existing trade: {Signature}", trade.Signature);
            }

            await context.SaveChangesAsync();
        }
    }
}
