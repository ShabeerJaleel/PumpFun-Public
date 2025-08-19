using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PumpFun.Core.Configuration;
using PumpFun.Core.Interfaces;
using TdLib;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Linq;
using PumpFun.Core.Models.Responses;

namespace PumpFun.Daemon.Services.Telegram;

public class PumpFunAnalysisService : TelegramService, IPumpFunAnalysisService
{
    private class PendingRequest
    {
        public string TokenAddress { get; set; } = "";
        public DateTime RequestTime { get; set; }
        public long MessageId { get; set; }
    }

    private long? _targetChannelId;
    private PendingRequest? _currentRequest;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private const int REQUEST_TIMEOUT_SECONDS = 120; // 2 minutes timeout
    private const int RETRY_DELAY_MS = 2000; // 5 seconds delay for retry
    public event EventHandler<TokenAnalysisResponseEventArgs>? OnTokenAnalysisReceived;
    private readonly TelegramConfig _telegramConfig;

    private readonly ILogger<PumpFunAnalysisService> _logger;

    public PumpFunAnalysisService(
        IOptions<TelegramConfig> telegramConfigOptions,
        ILogger<PumpFunAnalysisService> logger
    ) : base(telegramConfigOptions.Value)
    {
        _telegramConfig = telegramConfigOptions.Value;
        _logger = logger;
        // Initialize other dependencies
    }

    public async Task InitializeAsync()
    {
        await SetupTargetChannelAsync();
    }

    public async Task SubmitTokenAddressAsync(string tokenAddress)
    {
        if (string.IsNullOrWhiteSpace(tokenAddress))
            throw new ArgumentException("Token address cannot be empty", nameof(tokenAddress));
            
        if (!_targetChannelId.HasValue)
            throw new InvalidOperationException("Target channel not initialized");

        if (!await _requestLock.WaitAsync(TimeSpan.FromSeconds(1)))
        {
            throw new InvalidOperationException("Analysis already in progress. Please wait for it to complete.");
        }

        try
        {
            if (_currentRequest != null)
            {
                if ((DateTime.UtcNow - _currentRequest.RequestTime).TotalSeconds < REQUEST_TIMEOUT_SECONDS)
                {
                    throw new InvalidOperationException("Previous analysis still in progress");
                }
                _currentRequest = null;
            }

            var message = await Client.ExecuteAsync(new TdApi.SendMessage
            {
                ChatId = _targetChannelId.Value,
                InputMessageContent = new TdApi.InputMessageContent.InputMessageText
                {
                    Text = new TdApi.FormattedText { Text = tokenAddress }
                }
            });

            _currentRequest = new PendingRequest
            {
                TokenAddress = tokenAddress,
                RequestTime = DateTime.UtcNow,
                MessageId = message.Id
            };
        }
        finally
        {
            _requestLock.Release();
        }
    }

    public string? CurrentTokenAddress => _currentRequest?.TokenAddress;

    protected override async Task ProcessUpdatesAsync(TdApi.Update update)
    {
        await base.ProcessUpdatesAsync(update);

        try
        {
            switch (update)
            {
                case TdApi.Update.UpdateNewMessage { Message: var message }:
                    _logger.LogDebug("Received UpdateNewMessage for chat {ChatId}", message.ChatId);
                    if (message.ChatId == _targetChannelId)
                    {
                        //HandleChannelMessage(message);
                    }
                    break;

                case TdApi.Update.UpdateMessageEdited { ChatId: var chatId, MessageId: var messageId }:
                    _logger.LogDebug("Received UpdateMessageEdited for chat {ChatId}", chatId);
                    if (chatId == _targetChannelId)
                    {
                        var editedMessage = await Client.ExecuteAsync(new TdApi.GetMessage
                        {
                            ChatId = chatId,
                            MessageId = messageId
                        });
                        //HandleChannelMessage(editedMessage);
                    }
                    break;

                case TdApi.Update.UpdateChatLastMessage { ChatId: var chatId, LastMessage: var lastMessage }:
                    _logger.LogDebug("Received UpdateChatLastMessage for chat {ChatId}", chatId);
                    if (chatId == _targetChannelId && lastMessage != null)
                    {
                        HandleChannelMessage(lastMessage);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update: {UpdateType}", update.GetType().Name);
        }
    }

    private void HandleChannelMessage(TdApi.Message message)
    {
        if (message?.Content is not TdApi.MessageContent.MessageText textMessage || _currentRequest == null)
            return;

        var messageText = textMessage.Text.Text;
       
        _logger.LogInformation("Recieved message: {MessageText}", messageText);

        ProcessAnalysisResponse(message, messageText);
    }

    private void ProcessAnalysisResponse(TdApi.Message message, string messageText)
    {
        if (_currentRequest == null) return;

        // Skip initial scanning message
        if (messageText.Contains("Token found!") && messageText.Contains("Scanning the token"))
        {
            return;
        }

        // Handle scanning too fast message
        if (messageText.Contains("You're scanning too fast"))
        {
            _ = RetryTokenSubmissionAsync(_currentRequest.TokenAddress);
            return;
        }

        // Handle error or completion
        if (messageText.Contains("Something went wrong") || 
            messageText.Contains("is not listed on") ||
            (messageText.Contains("Analysis complete!") && messageText.Contains(_currentRequest.TokenAddress)))
        {
            var formatted = GetFormattedMessageText(message);
            _logger.LogInformation(formatted);
            OnTokenAnalysisReceived?.Invoke(this, new TokenAnalysisResponseEventArgs
            {
                TokenAddress = _currentRequest.TokenAddress,
                Analysis = messageText,
                IsError = messageText.Contains("Something went wrong") || 
                messageText.Contains("is not listed on"),
                Timestamp = DateTime.UtcNow
            });
            _currentRequest = null;
        }
    }

    private async Task RetryTokenSubmissionAsync(string tokenAddress)
    {
        try
        {
            await Task.Delay(RETRY_DELAY_MS);
            var message = await Client.ExecuteAsync(new TdApi.SendMessage
            {
                ChatId = _targetChannelId.Value,
                InputMessageContent = new TdApi.InputMessageContent.InputMessageText
                {
                    Text = new TdApi.FormattedText { Text = tokenAddress }
                }
            });

            // Update the current request with new message ID
            _currentRequest = new PendingRequest
            {
                TokenAddress = tokenAddress,
                RequestTime = DateTime.UtcNow,
                MessageId = message.Id
            };

        }
        finally
        {
            _requestLock.Release();
        }
    }

    private const string TARGET_CHANNEL = "@CHANNEL_NAME"; // Replace with actual channel username without @

    private async Task SetupTargetChannelAsync()
    {
        try
        {
            var chat = await Client.ExecuteAsync(new TdApi.SearchPublicChat
            {
                Username = TARGET_CHANNEL
            });
            
            _targetChannelId = chat.Id;
            
            if (!_targetChannelId.HasValue)
            {
                throw new InvalidOperationException($"Could not find channel @{TARGET_CHANNEL}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize channel @{TARGET_CHANNEL}: {ex.Message}");
        }
    }

    private string GetFormattedMessageText(TdApi.Message message)
    {
        if (message?.Content is not TdApi.MessageContent.MessageText textMessage)
            return string.Empty;

        var formattedText = textMessage.Text;
        var plainText = formattedText.Text;
        var entities = formattedText.Entities;

        if (string.IsNullOrEmpty(plainText) || entities == null || !entities.Any())
            return plainText;

        // Create position-based entity tracking
        var positions = new SortedDictionary<int, List<(int End, TdApi.TextEntityType Type)>>();
        
        // Map all entity positions
        foreach (var entity in entities)
        {
            var end = entity.Offset + entity.Length;
            if (!positions.ContainsKey(entity.Offset))
                positions[entity.Offset] = new List<(int, TdApi.TextEntityType)>();
            
            positions[entity.Offset].Add((end, entity.Type));
        }

        var result = new StringBuilder();
        var currentIndex = 0;
        var activeEntities = new List<(int End, TdApi.TextEntityType Type)>();

        try
        {
            foreach (var pos in positions)
            {
                var offset = pos.Key;
                
                // Add plain text before this position if needed
                if (offset > currentIndex)
                {
                    result.Append(plainText.Substring(currentIndex, offset - currentIndex));
                }

                // Close any active entities that end here
                activeEntities.RemoveAll(e => e.End <= offset);

                // Add new entities starting at this position
                activeEntities.AddRange(pos.Value);
                
                // Get next position or end of text
                var nextPos = positions.Keys.Where(k => k > offset).DefaultIfEmpty(plainText.Length).First();
                var length = nextPos - offset;

                if (length > 0)
                {
                    var text = plainText.Substring(offset, length);

                    // Apply all active entity formatting
                    foreach (var entity in activeEntities.OrderBy(e => e.End))
                    {
                        text = FormatEntityText(text, entity.Type);
                    }

                    result.Append(text);
                }
                else
                {
                    _logger.LogWarning("Skipping empty text at position {Position}", offset);
                }

                    currentIndex = offset + length;
            }

            // Add any remaining text
            if (currentIndex < plainText.Length)
            {
                result.Append(plainText.Substring(currentIndex));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting message text");
            return plainText; // Fallback to plain text on error
        }

        return result.ToString();
    }

    private string FormatEntityText(string text, TdApi.TextEntityType entityType)
    {
        return entityType switch
        {
            TdApi.TextEntityType.TextEntityTypeUrl => $"<a href=\"{text}\">{text}</a>",
            TdApi.TextEntityType.TextEntityTypeTextUrl textUrl => $"<a href=\"{textUrl.Url}\">{text}</a>",
            TdApi.TextEntityType.TextEntityTypeMention => $"<a href=\"https://t.me/{text.TrimStart('@')}\">{text}</a>",
            TdApi.TextEntityType.TextEntityTypeBold => $"<b>{text}</b>",
            TdApi.TextEntityType.TextEntityTypeItalic => $"<i>{text}</i>",
            TdApi.TextEntityType.TextEntityTypeCode => $"<code>{text}</code>",
            TdApi.TextEntityType.TextEntityTypePre => $"<pre>{text}</pre>",
            TdApi.TextEntityType.TextEntityTypePreCode preCode => $"<pre><code class=\"language-{preCode.Language}\">{text}</code></pre>",
            TdApi.TextEntityType.TextEntityTypeStrikethrough => $"<s>{text}</s>",
            TdApi.TextEntityType.TextEntityTypeUnderline => $"<u>{text}</u>",
            TdApi.TextEntityType.TextEntityTypeHashtag => $"<span class=\"hashtag\">{text}</span>",
            TdApi.TextEntityType.TextEntityTypeCashtag => $"<span class=\"cashtag\">{text}</span>",
            TdApi.TextEntityType.TextEntityTypeBankCardNumber => $"<span class=\"bank-card\">{text}</span>",
            TdApi.TextEntityType.TextEntityTypePhoneNumber => $"<a href=\"tel:{text}\">{text}</a>",
            TdApi.TextEntityType.TextEntityTypeEmailAddress => $"<a href=\"mailto:{text}\">{text}</a>",
            TdApi.TextEntityType.TextEntityTypeBotCommand => $"<span class=\"bot-command\">{text}</span>",
            TdApi.TextEntityType.TextEntityTypeMediaTimestamp mediaTimestamp => 
                $"<a href=\"t={mediaTimestamp.MediaTimestamp}\">{text}</a>",
            TdApi.TextEntityType.TextEntityTypeSpoiler => $"<span class=\"spoiler\">{text}</span>",
            TdApi.TextEntityType.TextEntityTypeCustomEmoji customEmoji => 
                $"<emoji emoji-id=\"{customEmoji.CustomEmojiId}\">{text}</emoji>",
            _ => text
        };
    }
}
