using System.Threading.Channels;
using PumpFun.Core.Models;

namespace PumpFun.Core.Interfaces
{
    public interface ITradeProcessingService
    {
        Task ProcessTradeQueue(TradeChannel queue, CancellationToken cancellationToken);
    }
}
