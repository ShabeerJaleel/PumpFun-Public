using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using PumpFun.Core.Models;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Interfaces.Telegram;
using PumpFun.Daemon.Services;

namespace PumpFun.Daemon;

public class DaemonService : BackgroundService
{
    private readonly ILogger<DaemonService> _logger;
    private readonly IWebSocketClient _webSocketClient;
    private readonly ITokenProcessingService _tokenProcessingService;
    private readonly TokenCreationChannel _tokenCreationQueue;
    private readonly ITradeProcessingService _tradeProcessingService;
    private readonly TradeChannel _tradeQueue;
    private readonly ISyraxBotService _syraxBotService;
    private readonly TelegramBroadcastService _telegramBroadcastService;

    public DaemonService(
        ILogger<DaemonService> logger,
        IWebSocketClient webSocketClient,
        ITokenProcessingService tokenProcessingService,
        ITradeProcessingService tradeProcessingService,
        TokenCreationChannel tokenCreationQueue,
        TradeChannel tradeQueue,
        ISyraxBotService syraxBotService,
        TelegramBroadcastService telegramBroadcastService)
    {
        _logger = logger;
        _webSocketClient = webSocketClient;
        _tokenProcessingService = tokenProcessingService;
        _tradeProcessingService = tradeProcessingService;
        _tokenCreationQueue = tokenCreationQueue;
        _tradeQueue = tradeQueue;
        _syraxBotService = syraxBotService;
        _telegramBroadcastService = telegramBroadcastService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DaemonService started.");

        try
        {
            // Initialize Telegram services
            _logger.LogInformation("Initializing Telegram services...");

            var syraxLoginSuccess = await _syraxBotService.LoginAsync();
            if (!syraxLoginSuccess)
            {
                _logger.LogError("Failed to authenticate Syrax bot.");
                return;
            }
            _logger.LogInformation("Syrax bot authenticated successfully.");

            // Start broadcasting service
            await _telegramBroadcastService.StartAsync();
            
            // Start token processing in the background
            var tokenProcessingTask = _tokenProcessingService.ProcessTokenCreationQueue(_tokenCreationQueue, stoppingToken);

            // Start trade processing in the background
            var tradeProcessingTask = _tradeProcessingService.ProcessTradeQueue(_tradeQueue, stoppingToken);

            // Connect to the WebSocket
            await _webSocketClient.ConnectAsync(stoppingToken);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in DaemonService.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping DaemonService...");
        await _webSocketClient.DisconnectAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
