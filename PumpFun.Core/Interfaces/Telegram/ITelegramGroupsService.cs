using System.Threading.Tasks;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models;

namespace PumpFun.Core.Interfaces.Telegram
{
    public interface ITelegramGroupsService : IWTelegramService
    {
        event EventHandler<TelegramMessageEventArgs>? OnMessageReceived;
    }
}
