using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PumpFun.Daemon.Services
{
    public class TradeProcessingService : ITradeProcessingService
    {
        private readonly ILogger<TradeProcessingService> _logger;
        private readonly ITradeRepository _tradeRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly ITokenAnalysisService _tokenAnalysisService;

        public TradeProcessingService(
            ILogger<TradeProcessingService> logger,
            ITradeRepository tradeRepository,
            ITokenRepository tokenRepository,
            ITokenAnalysisService tokenAnalysisService)
        {
            _logger = logger;
            _tradeRepository = tradeRepository;
            _tokenRepository = tokenRepository;
            _tokenAnalysisService = tokenAnalysisService;
        }

        public async Task ProcessTradeQueue(TradeChannel queue, CancellationToken cancellationToken)
        {
            await foreach (var transaction in queue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var trade = new Trade
                    {
                        Signature = transaction.Signature,
                        TokenAddress = transaction.Mint,
                        TraderPublicKey = transaction.TraderPublicKey,
                        TxType = transaction.TxType.ToString().ToLower(),
                        TokenAmount = transaction.TokenAmount ?? 0,
                        NewTokenBalance = transaction.NewTokenBalance ?? 0,
                        VTokensInBondingCurve = transaction.VTokensInBondingCurve,
                        VSolInBondingCurve = transaction.VSolInBondingCurve,
                        MarketCapSol = transaction.MarketCapSol
                    };

                    var token = await _tokenRepository.UpdateTokenMarketCapAsync(trade.TokenAddress, trade.MarketCapSol);
                    if(token != null)
                    {
                        await _tokenAnalysisService.EnqueueTokenForAnalysis(token);
                    }
                    else
                        _logger.LogError("Token not found for MC update: {TokenAddress}", trade.TokenAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing trade: {Symbol}, {TokenAddress}", transaction.Symbol, transaction.Mint);
                }
            }
        }
    }
}
