namespace PumpFun.Core.Interfaces
{
    public interface IWebSocketClient
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
        bool IsConnected { get; }
    }
}
