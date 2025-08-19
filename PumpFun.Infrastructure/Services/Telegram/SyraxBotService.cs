using TL;
using PumpFun.Core.Configuration;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Interfaces.Telegram;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Models.Responses;
using WTelegram;

namespace PumpFun.Infrastructure.Services
{
    public class SyraxBotService : WTelegramService, ISyraxBotService
    {
        private long _syraxBotId;
        private long _syraxBotAccessHash;  // Add this field
        private PendingRequest? _currentRequest;
        private readonly SemaphoreSlim _requestLock = new(1, 1);
        private const int REQUEST_TIMEOUT_SECONDS = 120; // 2 minutes timeout
        private const int RETRY_DELAY_MS = 2000; // 2 seconds delay for retry

        public event EventHandler<TokenAnalysisResponseEventArgs>? OnTokenAnalysisReceived;

        public SyraxBotService(TelegramConfig config, ILogger<SyraxBotService> logger) 
            : base(config, logger)
        {
        }

        public override async Task<bool> LoginAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to login to Syrax bot service");
                var success = await base.LoginAsync();
                if (!success)
                {
                    _logger.LogError("Failed to login to Telegram service");
                    return false;
                }

                await InitializeBotAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during login process");
                throw new InvalidOperationException("Failed to initialize Syrax bot service", ex);
            }
        }

        private async Task InitializeBotAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Syrax bot with username: {Username}", Config.BotUsername);
                var bot = await _client.Contacts_ResolveUsername(Config.BotUsername);
                
                if (bot?.User == null)
                {
                    throw new InvalidOperationException($"Could not resolve bot username: {Config.BotUsername}");
                }

                var botUser = (User)bot.User;
                _syraxBotId = botUser.id;
                _syraxBotAccessHash = botUser.access_hash;

                _logger.LogInformation("Successfully initialized Syrax bot. ID: {BotId}", _syraxBotId);
                RegisterUpdateHandler(HandleUpdateAsync, includeIds: new List<long> { _syraxBotId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Syrax bot with username: {Username}", Config.BotUsername);
                throw new InvalidOperationException("Bot initialization failed", ex);
            }
        }

        private class PendingRequest
        {
            public string TokenAddress { get; set; } = "";
            public DateTime RequestTime { get; set; }
            public long MessageId { get; set; }
        }

        public async Task SubmitTokenAddressAsync(string tokenAddress)
        {
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                _logger.LogError("Attempted to submit empty token address");
                throw new ArgumentException("Token address cannot be empty", nameof(tokenAddress));
            }

            try
            {
                if (!await _requestLock.WaitAsync(TimeSpan.FromSeconds(1)))
                {
                    _logger.LogWarning("Lock acquisition timeout for token: {TokenAddress}", tokenAddress);
                    throw new InvalidOperationException("Analysis already in progress");
                }

                try
                {
                    if (_currentRequest != null)
                    {
                        var elapsedTime = (DateTime.UtcNow - _currentRequest.RequestTime).TotalSeconds;
                        if (elapsedTime < REQUEST_TIMEOUT_SECONDS)
                        {
                            _logger.LogWarning("Previous analysis still in progress for: {CurrentToken}", _currentRequest.TokenAddress);
                            throw new InvalidOperationException("Previous analysis still in progress");
                        }
                        _logger.LogInformation("Previous request timed out, clearing for new analysis");
                        _currentRequest = null;
                    }

                    var inputPeer = new InputPeerUser(_syraxBotId, _syraxBotAccessHash);
                    var message = await _client.SendMessageAsync(inputPeer, tokenAddress);

                    _currentRequest = new PendingRequest
                    {
                        TokenAddress = tokenAddress,
                        RequestTime = DateTime.UtcNow,
                        MessageId = message.id
                    };
                }
                finally
                {
                    _requestLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit token for analysis: {TokenAddress}", tokenAddress);
                throw;
            }
        }

        private Task HandleUpdateAsync(MessageBase message)
        {
            ProcessSyraxMessageAsync((Message)message);
            return Task.CompletedTask;
        }

        private void ProcessSyraxMessageAsync(Message message)
        {
            try
            {
                if (_currentRequest == null)
                {
                    _logger.LogDebug("Received message with no current request: {MessageText}", message.message);
                    return;
                }

                string messageText = message.message;

                if (messageText.Contains("Token found!") && messageText.Contains("Scanning the token"))
                {
                    return;
                }

                // Add handling for scanning too fast
                if (messageText.Contains("You're scanning too fast"))
                {
                    _ = RetryTokenSubmissionAsync(_currentRequest.TokenAddress);
                    return;
                }

                if (IsCompletionMessage(messageText))
                {
                    var isError = messageText.Contains("Something went wrong") || 
                                 messageText.Contains("is not listed on");

                    var eventArgs = new TokenAnalysisResponseEventArgs
                    {
                        TokenAddress = _currentRequest.TokenAddress,
                        Analysis = messageText,
                        FormattedAnalysis = _client.EntitiesToHtml(messageText, message.entities),
                        IsError = isError,
                        Timestamp = DateTime.UtcNow
                    };

                    // Fire event asynchronously
                    _ = Task.Run(() => OnTokenAnalysisReceived?.Invoke(this, eventArgs));

                    _currentRequest = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for token: {TokenAddress}", 
                    _currentRequest?.TokenAddress);
                throw;
            }
        }

        private async Task RetryTokenSubmissionAsync(string tokenAddress)
        {
            try
            {
                await Task.Delay(RETRY_DELAY_MS);
                var inputPeer = new InputPeerUser(_syraxBotId, _syraxBotAccessHash);
                var message = await _client.SendMessageAsync(inputPeer, tokenAddress);

                _currentRequest = new PendingRequest
                {
                    TokenAddress = tokenAddress,
                    RequestTime = DateTime.UtcNow,
                    MessageId = message.id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry token submission for: {TokenAddress}", tokenAddress);
                throw;
            }
        }

        private bool IsCompletionMessage(string messageText)
        {
            return messageText.Contains("Something went wrong") ||
                   messageText.Contains("is not listed on") ||
                   (messageText.Contains("Analysis complete!") && 
                    messageText.Contains(_currentRequest?.TokenAddress ?? ""));
        }

        public string? CurrentTokenAddress => _currentRequest?.TokenAddress;
    }
}