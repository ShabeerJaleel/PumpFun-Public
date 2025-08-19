using PumpFun.Core.Models;

namespace PumpFun.Core.Interfaces
{
    public interface ITradeRepository
    {
        Task AddTradeAsync(Trade trade);
    }
}
