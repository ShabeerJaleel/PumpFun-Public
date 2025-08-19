using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models;
using System.Threading.Tasks;
using System;

namespace PumpFun.Daemon.Services
{
    public class TokenProcessingService : ITokenProcessingService
    {
        private readonly ILogger<TokenProcessingService> _logger;
        private readonly IIpfsService _ipfsService;
        private readonly ITokenRepository _tokenRepository;

        public TokenProcessingService(
            ILogger<TokenProcessingService> logger,
            IIpfsService ipfsService,
            ITokenRepository tokenRepository)
        {
            _logger = logger;
            _ipfsService = ipfsService;
            _tokenRepository = tokenRepository;
        }

        public async Task ProcessTokenCreationQueue(
            TokenCreationChannel queue,
            CancellationToken cancellationToken)
        {
            await foreach (var transaction in queue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // First save the token with transaction data
                    var initialToken = new Token
                    {
                        TokenAddress = transaction.Mint,
                        Symbol = transaction.Symbol,
                        Name = transaction.Name,
                        InitialBuy = transaction.InitialBuy ?? 0,
                        VTokens = transaction.VTokensInBondingCurve,
                        VSol = transaction.VSolInBondingCurve,
                        MarketCapSol = transaction.MarketCapSol,
                    };

                    await _tokenRepository.AddOrUpdateTokenAsync(initialToken);
                  
                    // Then fetch and update with IPFS data
                    try
                    {
                        var tokenInfo = await _ipfsService.GetTokenInfoAsync(transaction.Uri, cancellationToken);
                        initialToken.Description = tokenInfo.Description ?? string.Empty;
                        initialToken.Image = tokenInfo.Image;
                        initialToken.Twitter = tokenInfo.Twitter ?? string.Empty;
                        initialToken.Telegram = tokenInfo.Telegram ?? string.Empty;
                        initialToken.Website = tokenInfo.Website ?? string.Empty;

                        await _tokenRepository.UpdateTokenAsync(initialToken);
                        _logger.LogInformation("Saved token with IPFS data: {Symbol}, {TokenAddress}", 
                            initialToken.Symbol, initialToken.TokenAddress);
                    }
                    catch (Exception ipfsEx)
                    {
                        _logger.LogError(ipfsEx, "Error updating IPFS data for token: {Symbol}, {TokenAddress}", 
                            transaction.Symbol, transaction.Mint);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving token: {Symbol}, {TokenAddress}", 
                        transaction.Symbol, transaction.Mint);
                }
            }
        }
    }
}