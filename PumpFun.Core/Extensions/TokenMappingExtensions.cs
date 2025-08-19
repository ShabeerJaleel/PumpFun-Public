using PumpFun.Core.Models;
using PumpFun.Core.Models.Dtos;

namespace PumpFun.Core.Extensions
{
    public static class TokenMappingExtensions
    {
        public static TokenDto ToDto(this Token token, decimal marketCapUsd)
        {
            return new TokenDto
            {
                TokenAddress = token.TokenAddress,
                Symbol = token.Symbol,
                Name = token.Name,
                Description = token.Description,
                Image = token.Image,
                Twitter = token.Twitter,
                Telegram = token.Telegram,
                Website = token.Website,
                InitialBuy = token.InitialBuy,
                VTokens = token.VTokens,
                VSol = token.VSol,
                MarketCapSol = token.MarketCapSol,
                MarketCap = marketCapUsd,
                CreatedAt = token.CreatedAt,
                Analysis = token.Analysis,
                AnalysisCompleted = token.AnalysisCompleted,
                AnalysisTimestamp = token.AnalysisTimestamp
            };
        }
    }
}
