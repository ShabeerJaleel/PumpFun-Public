using PumpFun.Core.Models.Requests;
using PumpFun.Core.Models.Responses;

namespace PumpFun.Core.Interfaces
{
    public interface ITokenCreationService
    {
        Task<TokenCreationResponse> CreateTokenAsync(TokenCreationRequest request, CancellationToken cancellationToken);
    }
}
