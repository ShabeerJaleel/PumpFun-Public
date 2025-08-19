using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces;
using System.Net.Http.Json;

namespace PumpFun.Infrastructure.Services
{
    public class SolPriceService : ISolPriceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SolPriceService> _logger;
        private static decimal? _currentSolPrice;
        private static DateTime _lastUpdate = DateTime.MinValue;
        private static readonly SemaphoreSlim _lock = new(1, 1);
        
        private const string COINGECKO_API_URL = 
            "https://api.coingecko.com/api/v3/simple/price?ids=solana&vs_currencies=usd";

        public SolPriceService(
            IHttpClientFactory httpClientFactory,
            ILogger<SolPriceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await UpdateSolPriceAsync();
        }

        public async Task<decimal> GetCurrentSolPriceAsync()
        {
            // Ensure we have initial price
            if (!_currentSolPrice.HasValue)
            {
                await UpdateSolPriceAsync();
                // If still null after update, use default
                if (!_currentSolPrice.HasValue)
                    _currentSolPrice = 200;
            }
            else if (DateTime.UtcNow - _lastUpdate > TimeSpan.FromMinutes(5))
            {
                // Fire and forget
                _ = Task.Run(UpdateSolPriceAsync);
            }

            return _currentSolPrice.Value;
        }

        public async Task<decimal> ConvertSolToUsd(decimal solAmount)
        {
            var price = await GetCurrentSolPriceAsync();
            return solAmount * price;
        }

        public async Task<decimal> ConvertUsdToSol(decimal usdAmount)
        {
            var price = await GetCurrentSolPriceAsync();
            return usdAmount / price;
        }

        private async Task UpdateSolPriceAsync()
        {
            if (!await _lock.WaitAsync(TimeSpan.Zero))
                return;

            try
            {
                // Double check if update is still needed
                if (_currentSolPrice.HasValue && DateTime.UtcNow - _lastUpdate <= TimeSpan.FromMinutes(5))
                    return;

                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(COINGECKO_API_URL);
                
                if (response != null && response.TryGetValue("solana", out var prices) && 
                    prices.TryGetValue("usd", out var price))
                {
                    _currentSolPrice = price;
                    _lastUpdate = DateTime.UtcNow;
                    _logger.LogInformation("Updated SOL price: ${Price}", price);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SOL price");
                
                // Don't throw - use old price if available
            }
            finally
            {
                if (!_currentSolPrice.HasValue)
                    _currentSolPrice = 200;
                _lock.Release();
            }
        }
    }
}