using System;
using System.Threading.Tasks;
using PumpFun.Core.Interfaces.Telegram;
using PumpFun.Core.Models.Responses;

namespace PumpFun.Core.Interfaces.Telegram
{
    public interface ISyraxBotService : IWTelegramService
    {
        Task SubmitTokenAddressAsync(string tokenAddress);
        string? CurrentTokenAddress { get; }
        event EventHandler<TokenAnalysisResponseEventArgs> OnTokenAnalysisReceived;
    }
}
