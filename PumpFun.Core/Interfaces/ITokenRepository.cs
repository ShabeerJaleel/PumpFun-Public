using System.Threading.Tasks;
using PumpFun.Core.Models;
using PumpFun.Core.Models.Requests;

namespace PumpFun.Core.Interfaces
{
    public interface ITokenRepository
    {
        Task AddOrUpdateTokenAsync(Token token);
        Task<Token?> GetTokenAsync(string tokenAddress);
        Task<IEnumerable<Token>> GetTokensAsync(TokenFilterRequest filter);
        Task<Token> UpdateTokenMarketCapAsync(string tokenAddress, decimal marketCap);
        Task UpdateTokenAnalysisAsync(string tokenAddress, string analysis);
        Task UpdateTokenAsync(Token token);
    }
}