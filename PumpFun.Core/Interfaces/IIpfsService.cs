using PumpFun.Core.Models;

namespace PumpFun.Core.Interfaces
{
    public interface IIpfsService
    {
        Task<IpfsTokenInfo> GetTokenInfoAsync(string ipfsUrl, CancellationToken cancellationToken);
        Task<byte[]> GetImageDataAsync(string imageUrl, CancellationToken cancellationToken);
    }
}
