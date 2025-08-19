using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WTelegram;
using TL;
using PumpFun.Core.Configuration;
using PumpFun.Core.Interfaces;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces.Telegram;

namespace PumpFun.Infrastructure.Services
{
    public abstract class WTelegramService : IWTelegramService, IDisposable
    {
        protected static Client _client;
        private static readonly object _lock = new();
        private static readonly List<UpdateHandlerRegistration> _updateHandlers = new();
        protected static ILogger? _logger;  // Add static logger

        protected readonly TelegramConfig Config;

        protected WTelegramService(TelegramConfig config, ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_client == null)
            {
                lock (_lock)
                {
                    if (_client == null)
                    {
                        _client = new Client(ConfigValues);
                        _client.WithUpdateManager(OnClientUpdateReceived);
                        _logger.LogInformation("WTelegram client initialized");
                    }
                }
            }
        }

        private string ConfigValues(string what)
        {
            return what switch
            {
                "api_id"       => Config.ApiId,
                "api_hash"     => Config.ApiHash,
                "phone_number" => Config.PhoneNumber,
                _ => null,
            };
        }

        public virtual async Task<bool> LoginAsync()
        {
            await _client.LoginUserIfNeeded();
            return true;
        }

        // Modify to static
        protected static void RegisterUpdateHandler(Func<MessageBase, Task> handler, List<long>? includeIds = null, List<long>? excludeIds = null)
        {
            var registration = new UpdateHandlerRegistration
            {
                Handler = handler,
                IncludeIds = includeIds ?? new List<long>(),
                ExcludeIds = excludeIds ?? new List<long>()
            };
            _updateHandlers.Add(registration);
        }

        // Make private methods static as well since they work with static data
        private static async Task OnClientUpdateReceived(Update update)
        {
            foreach (var registration in _updateHandlers)
            {
                if (ShouldHandleUpdate(update, registration))
                {
                    try
                    {
                        switch (update)
                        {
                            case UpdateNewChannelMessage ucm: await registration.Handler(ucm.message); break;
                            case UpdateNewMessage unm: await registration.Handler(unm.message); break;
                            case UpdateEditChannelMessage ecm: await registration.Handler(ecm.message); break;
                            case UpdateEditMessage em: await registration.Handler(em.message); break;
                            default:
                                _logger?.LogWarning(update.ToString());
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing update: {UpdateType}", update.GetType().Name);
                    }
                }
            }
        }

        private static bool ShouldHandleUpdate(Update update, UpdateHandlerRegistration registration)
        {
            var senderId = GetSenderId(update);
            if (senderId == 0)
                return false;

            return registration.IncludeIds.Count > 0 
                ? registration.IncludeIds.Contains(senderId)
                : !registration.ExcludeIds.Contains(senderId);
        }

        // Extract sender ID from the update
        private static long GetSenderId(Update update)
        {
            switch (update)
            {
                case UpdateNewChannelMessage ucm: return ucm.message.Peer.ID;
                case UpdateNewMessage unm: return unm.message.Peer.ID;
                case UpdateEditChannelMessage ecm: return ecm.message.Peer.ID;
                case UpdateEditMessage em: return em.message.Peer.ID;
                default: return 0;
            }
        }
           
        public void Dispose()
        {
            _client.Dispose();
        }

        private class UpdateHandlerRegistration
        {
            public Func<MessageBase, Task> Handler { get; set; } = null!;
            public List<long> IncludeIds { get; set; } = new();
            public List<long> ExcludeIds { get; set; } = new();
        }
    }
}
