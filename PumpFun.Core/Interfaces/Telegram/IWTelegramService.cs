using System.Threading.Tasks;

namespace PumpFun.Core.Interfaces.Telegram
{
    public interface IWTelegramService
    {
        Task<bool> LoginAsync();
    }
}
