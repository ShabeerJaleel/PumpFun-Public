// File: src/PumpFun.Core/Services/PumpPortal/PumpPortalWebSocketClient.cs
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Models;
using PumpFun.Core.Interfaces;
using System.Collections.Concurrent;

namespace PumpFun.Daemon.Services.PumpPortal
{
    public class PumpPortalWebSocketClient : IWebSocketClient
    {
        private readonly ILogger<PumpPortalWebSocketClient> _logger;
        private ClientWebSocket _webSocket;
        private readonly Channel<string> _subscriptionQueue;
        private readonly TokenCreationChannel _tokenCreationQueue;
        private readonly TradeChannel _tradeQueue;
        private const string WebSocketUrl = "wss://pumpportal.fun/api/data";
        private const int BUFFER_SIZE = 1024 * 16;
        private const int RECONNECT_DELAY_MS = 2000;

        // Use ConcurrentQueue with fixed size for thread-safety
        private readonly ConcurrentQueue<string> _recentTokens = new();
        private const int MAX_RECENT_TOKENS = 100;

        private readonly ConcurrentDictionary<string, TransactionModel> _latestTrades = new();
        private readonly System.Timers.Timer _tradeProcessingTimer;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        public PumpPortalWebSocketClient(
            ILogger<PumpPortalWebSocketClient> logger, 
            TokenCreationChannel tokenCreationQueue,
            TradeChannel tradeQueue)
        {
            _logger = logger;
            _webSocket = new ClientWebSocket();
            _subscriptionQueue = Channel.CreateUnbounded<string>();
            _tokenCreationQueue = tokenCreationQueue;
            _tradeQueue = tradeQueue;

            _tradeProcessingTimer = new System.Timers.Timer(3000);
            _tradeProcessingTimer.Elapsed += async (s, e) => await ProcessLatestTradesAsync();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _webSocket.ConnectAsync(new Uri(WebSocketUrl), cancellationToken);

                // Start subscription processor
                _ = ProcessSubscriptionsAsync(cancellationToken);

                // Subscribe to new token events
                await SendSubscriptionMessage("subscribeNewToken", null, cancellationToken);
                _logger.LogInformation("Connected to PumpPortal WebSocket and subscribed to new token events");

                _tradeProcessingTimer.Start();
                await ReceiveMessagesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PumpPortal WebSocket connection error");
                await ReconnectAsync(cancellationToken);
            }
        }

        private async Task ProcessSubscriptionsAsync(CancellationToken cancellationToken)
        {
            await foreach (var tokenMint in _subscriptionQueue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await SendSubscriptionMessage("subscribeTokenTrade", new[] { tokenMint }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error subscribing to token: {mint}", tokenMint);
                }
            }
        }

        private async Task SendSubscriptionMessage(string method, string[]? keys, CancellationToken cancellationToken)
        {
            var message = new { method, keys };
            var messageJson = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(messageJson);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken
            );
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[BUFFER_SIZE];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        cancellationToken
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        //_logger.LogInformation("Received raw message: {message}", message);
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(message);

                        // Check if it's a subscription confirmation
                        if (jsonElement.TryGetProperty("message", out var messageElement))
                        {
                            // _logger.LogInformation("Subscription message: {message}", messageElement.GetString());
                        }
                        else
                        {
                            var transaction = TransactionMessageDeserializer.DeserializeTransactionMessage(message);
                            if (transaction?.TxType == TransactionType.Create)
                            {
                                await HandleTokenCreation(transaction, cancellationToken);
                            }
                            else if (transaction?.TxType == TransactionType.Buy ||
                             transaction?.TxType == TransactionType.Sell)
                            {
                                _latestTrades.AddOrUpdate(
                                    transaction.Mint,
                                    transaction,
                                    (key, oldValue) => transaction
                                );
                            }
                        }
                    }
                }
                catch (WebSocketException wsEx)
                {
                    _logger.LogError(wsEx, "WebSocket connection error");
                    await ReconnectAsync(cancellationToken);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Invalid message format received");
                }
            }
        }

        private async Task HandleTokenCreation(TransactionModel transaction, CancellationToken cancellationToken)
        {
            // Check for duplicates
            if (IsDuplicateToken(transaction.Mint))
            {
                _logger.LogWarning("Duplicate token creation detected: {Symbol}, {Mint}", 
                    transaction.Symbol, transaction.Mint);
                return;
            }

            // Add to recent tokens queue
            AddToRecentTokens(transaction.Mint);

            // Process as normal
            await _tokenCreationQueue.Writer.WriteAsync(transaction, cancellationToken);
            await _subscriptionQueue.Writer.WriteAsync(transaction.Mint, cancellationToken);
            _logger.LogInformation("NEW token: {symbol}, {mint}", 
                transaction.Symbol, transaction.Mint);
        }

        private bool IsDuplicateToken(string mint)
        {
            return _recentTokens.Contains(mint);
        }

        private void AddToRecentTokens(string mint)
        {
            _recentTokens.Enqueue(mint);
            
            // Keep queue size limited
            while (_recentTokens.Count > MAX_RECENT_TOKENS)
            {
                _recentTokens.TryDequeue(out _);
            }
        }

        private async Task ReconnectAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Attempting to reconnect to PumpPortal WebSocket...");
                    _webSocket.Dispose();
                    _webSocket = new ClientWebSocket();
                    await _webSocket.ConnectAsync(new Uri(WebSocketUrl), cancellationToken);
                    _logger.LogInformation("Reconnected to PumpPortal WebSocket");

                    // Resubscribe after reconnection
                    await SendSubscriptionMessage("subscribeNewToken", null, cancellationToken);
                    _logger.LogInformation("Resubscribed to new token events");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reconnection attempt failed. Retrying in {Delay}ms...", RECONNECT_DELAY_MS);
                    await Task.Delay(RECONNECT_DELAY_MS, cancellationToken);
                }
            }
        }

        private async Task ProcessLatestTradesAsync()
        {
            try
            {
                var tradesToProcess = _latestTrades.ToList();
                _logger.LogInformation("ProcessLatestTradesAsync starting. Total to process: {Count}", tradesToProcess.Count);

                foreach (var kvp in tradesToProcess)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                    {
                        _logger.LogWarning("Skipping trade processing due to null/empty key");
                        continue;
                    }

                    try
                    {
                        if (_latestTrades.TryRemove(kvp.Key, out var trade) && trade != null)
                        {
                            await _tradeQueue.Writer.WriteAsync(trade);
                        }
                    }
                    catch (Exception tradeEx)
                    {
                        _logger.LogError(tradeEx, "Error processing trade for key: {Key}", kvp.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessLatestTradesAsync");
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            _tradeProcessingTimer.Dispose();
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing PumpPortal connection",
                    cancellationToken
                );
            }
            _webSocket.Dispose();
        }
    }
}
