using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces.Telegram;
using PumpFun.Core.Models;
using System.IO.Pipes;
using System.Text.Json;

namespace PumpFun.Daemon.Services
{
    public class TelegramBroadcastService : IDisposable
    {
        private const string PIPE_NAME = "TelegramMessagePipe";
        private readonly ILogger<TelegramBroadcastService> _logger;
        private readonly ITelegramGroupsService _telegramGroupsService;
        private NamedPipeServerStream? _pipeServer;
        private StreamWriter? _pipeWriter;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _maxRetryDelay = TimeSpan.FromSeconds(30);
        private Task? _pipeServerTask;
        private volatile bool _isConnected;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public TelegramBroadcastService(
            ILogger<TelegramBroadcastService> logger,
            ITelegramGroupsService telegramGroupsService)
        {
            _logger = logger;
            _telegramGroupsService = telegramGroupsService;
        }

        public async Task StartAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            var loginSuccess = await _telegramGroupsService.LoginAsync();
            if (!loginSuccess)
            {
                _logger.LogError("Failed to authenticate Groups service.");
                throw new Exception("Failed to authenticate Telegram Groups service");
            }

            _telegramGroupsService.OnMessageReceived += HandleTelegramMessage;
            _pipeServerTask = InitializePipeServer(_cancellationTokenSource.Token);
        }

        private async Task InitializePipeServer(CancellationToken cancellationToken)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(
                    PIPE_NAME,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                _logger.LogInformation("Waiting for pipe client connection...");
                await _pipeServer.WaitForConnectionAsync(cancellationToken);
                _pipeWriter = new StreamWriter(_pipeServer) { AutoFlush = true };
                _logger.LogInformation("Pipe client connected");
                _isConnected = true;

                // Keep the task alive while connected
                while (_pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pipe server connection");
                throw;
            }
        }

        private async void HandleTelegramMessage(object? sender, TelegramMessageEventArgs e)
        {
            if (!_isConnected || _pipeWriter == null)
            {
                _logger.LogWarning("Message received but pipe is not connected");
                return;
            }

            try
            {
                await _writeLock.WaitAsync();
                try
                {
                    var messageJson = JsonSerializer.Serialize(e) + "\n";
                    await _pipeWriter.WriteAsync(messageJson);
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to telegram pipe");
            }
        }

        private void DisposePipe()
        {
            _pipeWriter?.Dispose();
            _pipeServer?.Dispose();
            _pipeWriter = null;
            _pipeServer = null;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _telegramGroupsService.OnMessageReceived -= HandleTelegramMessage;
            DisposePipe();
            _connectionLock.Dispose();
            _writeLock.Dispose();
            _pipeServerTask?.Wait(TimeSpan.FromSeconds(5));
        }
    }
}
