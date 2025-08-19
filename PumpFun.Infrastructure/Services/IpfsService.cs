using System.Text.Json;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models;

namespace PumpFun.Infrastructure.Services
{
    public class IpfsService : IIpfsService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IpfsService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IpfsTokenInfo> GetTokenInfoAsync(string ipfsUrl, CancellationToken cancellationToken)
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync(ipfsUrl, cancellationToken);
            var tokenInfo = JsonSerializer.Deserialize<IpfsTokenInfo>(response);
            if (tokenInfo == null)
                throw new Exception("Failed to deserialize token info");
            return tokenInfo;
        }

        public async Task<byte[]> GetImageDataAsync(string imageUrl, CancellationToken cancellationToken)
        {
            using var client = _httpClientFactory.CreateClient();
            return await client.GetByteArrayAsync(imageUrl, cancellationToken);
        }
    }
}
