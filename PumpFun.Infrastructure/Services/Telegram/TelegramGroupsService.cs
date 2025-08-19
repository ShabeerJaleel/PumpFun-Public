using TL;
using PumpFun.Core.Configuration;
using PumpFun.Core.Interfaces.Telegram;
using PumpFun.Core.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using WTelegram;

namespace PumpFun.Infrastructure.Services
{
    public class TelegramGroupsService : WTelegramService, ITelegramGroupsService
    {
        public event EventHandler<TelegramMessageEventArgs>? OnMessageReceived;

        private readonly Dictionary<long, string> _groupMapping = new();

        public TelegramGroupsService(TelegramConfig config, ILogger<TelegramGroupsService> logger) 
            : base(config, logger)
        {
        }

        public override async Task<bool> LoginAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to login to Telegram Groups service");
                var success = await base.LoginAsync();
                if (!success)
                {
                    _logger.LogError("Failed to login to Telegram service");
                    return false;
                }

                await ListenToChannelsAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during login process");
                throw;
            }
        }

        private async Task ListenToChannelsAsync()
        {
            try
            {
                var groupIds = new List<long>();
                foreach (var groupName in Config.GroupNames)
                {
                    try
                    {
                        var resolved = await _client.Contacts_ResolveUsername(groupName);
                        groupIds.Add(resolved.peer.ID);
                        _groupMapping[resolved.peer.ID] = groupName;
                        _logger.LogInformation("Successfully resolved group: {GroupName} with ID: {ChannelId}",
                                groupName, resolved.peer.ID);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to resolve group: {GroupName}", groupName);
                    }
                }

                if (groupIds.Count > 0)
                {
                    RegisterUpdateHandler(HandleMessageAsync, includeIds: groupIds);
                    _logger.LogInformation("Registered handlers for {Count} groups", groupIds.Count);
                }
                else
                {
                    _logger.LogWarning("No valid groups were found to monitor");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize group monitoring");
                throw;
            }
        }

        private async Task HandleMessageAsync(MessageBase messageBase)
        {
            try
            {
                if (messageBase is Message message)
                {
                    var imageLinks = new List<string>();

                 
                    var eventArgs = new TelegramMessageEventArgs
                    {
                        Message = message.message,
                        FormattedMessage = _client.EntitiesToHtml(message.message, message.entities),
                        ChatId = message.peer_id.ID,
                        MessageId = message.ID,
                        Timestamp = message.date,
                        GroupName = _groupMapping.TryGetValue(message.peer_id.ID, out var groupName) ? groupName : string.Empty,
                    };

                    if (message.media is MessageMediaPhoto { photo: Photo photo })
                    {
                        using var memoryStream = new MemoryStream();
                        await _client.DownloadFileAsync(photo, memoryStream);
                        var base64Image = Convert.ToBase64String(memoryStream.ToArray());
                        eventArgs.Images.Add($"data:image/jpeg;base64,{base64Image}");
                    }

                    if (message.entities?.Any(e => e is MessageEntityBlockquote) == true)
                    {
                        var blockquotes = message.entities
                            .Where(e => e is MessageEntityBlockquote)
                            .Select(blockquote => message.message.Substring(
                                blockquote.offset,
                                blockquote.length
                            ))
                            .ToList();
                  
                    }

                    if(message.fwd_from != null)
                    {
                        _logger.LogInformation(groupName);
                    }

                    // Fire event asynchronously
                    _ = Task.Run(() => OnMessageReceived?.Invoke(this, eventArgs));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing group message");
            }
        }
    }
}
