using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces.Telegram;
using PumpFun.Core.Models;
using PumpFun.Web.Hubs;
using System.IO.Pipes;
using System.Text.Json;

namespace PumpFun.Web.Services
{
    public class TelegramMessageService : BackgroundService
    {
        private const string PIPE_NAME = "TelegramMessagePipe";
        private readonly IHubContext<TelegramHub> _hubContext;
        private readonly ILogger<TelegramMessageService> _logger;
        private readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _maxRetryDelay = TimeSpan.FromSeconds(30);

        public TelegramMessageService(
            IHubContext<TelegramHub> hubContext,
            ILogger<TelegramMessageService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var currentDelay = _initialRetryDelay;

            while (!stoppingToken.IsCancellationRequested)
            {
                using var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.In);
                
                try
                {
                    _logger.LogInformation("Attempting to connect to telegram message pipe...");
                    await pipeClient.ConnectAsync(stoppingToken);
                    _logger.LogInformation("Connected to telegram message pipe");
                    
                    // Reset delay on successful connection
                    currentDelay = _initialRetryDelay;

                    using var reader = new StreamReader(pipeClient);
                    while (!stoppingToken.IsCancellationRequested && pipeClient.IsConnected)
                    {
                        var messageJson = await reader.ReadLineAsync();
                        if (messageJson == null) break;

                        var message = JsonSerializer.Deserialize<TelegramMessageEventArgs>(messageJson);
                        if (message != null)
                        {
                            _logger.LogInformation("Sending message to clients: {Message}", messageJson);
                            await _hubContext.Clients.All.SendAsync("ReceiveTelegramMessage", message, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in telegram pipe connection/reading");
                    
                    // Implement exponential backoff
                    await Task.Delay(currentDelay, stoppingToken);
                    currentDelay = TimeSpan.FromMilliseconds(Math.Min(
                        currentDelay.TotalMilliseconds * 2,
                        _maxRetryDelay.TotalMilliseconds
                    ));
                    
                    _logger.LogInformation("Retrying connection in {delay} seconds", currentDelay.TotalSeconds);
                }
            }
        }
    }
}