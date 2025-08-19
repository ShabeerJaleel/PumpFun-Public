using PumpFun.Core.Models.Responses;

namespace PumpFun.Core.Interfaces;

public interface IPumpFunAnalysisService : ITelegramService
{
    Task InitializeAsync();
    Task SubmitTokenAddressAsync(string tokenAddress);
    string? CurrentTokenAddress { get; }
    event EventHandler<TokenAnalysisResponseEventArgs> OnTokenAnalysisReceived;
}
