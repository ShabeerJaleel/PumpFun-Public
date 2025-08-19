using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PumpFun.Core.Configuration;
using PumpFun.Core.Interfaces;
using TdLib;
using TdLib.Bindings;

namespace PumpFun.Daemon.Services.Telegram;

public class TelegramService : ITelegramService
{
    private readonly TdClient _client;
    protected readonly TelegramConfig _config;  // Changed to protected
    private readonly ManualResetEventSlim _readyToAuthenticate;
    private bool _authNeeded;
    private bool _passwordNeeded;
    
    public TelegramService(TelegramConfig config)
    {
        _config = config;
        _client = new TdClient();
         _client.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);
        _readyToAuthenticate = new ManualResetEventSlim();
        _client.UpdateReceived += async (_, update) => await ProcessUpdatesAsync(update);
    }

    public virtual async Task<bool> LoginAsync()
    {
       _readyToAuthenticate.Wait();

        if (_authNeeded)
        {
            await _client.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber
            {
                PhoneNumber = _config.PhoneNumber
            });

            Console.Write("Insert the login code: ");
            var code = Console.ReadLine();

            await _client.ExecuteAsync(new TdApi.CheckAuthenticationCode
            {
                Code = code
            });

            if (_passwordNeeded)
            {
                Console.Write("Insert the password: ");
                var password = Console.ReadLine();

                await _client.ExecuteAsync(new TdApi.CheckAuthenticationPassword
                {
                    Password = password
                });
            }
        }

        return true;
    }

    public virtual async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var me = await _client.ExecuteAsync(new TdApi.GetMe());
            return me != null;
        }
        catch
        {
            return false;
        }
    }

    protected virtual async Task ProcessUpdatesAsync(TdApi.Update update)
    {
        switch (update)
        {
            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters }:
                await InitializeTdLibAsync();
                break;

            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber }:
            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitCode }:
                _authNeeded = true;
                _readyToAuthenticate.Set();
                break;

            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPassword }:
                _authNeeded = true;
                _passwordNeeded = true;
                _readyToAuthenticate.Set();
                break;

            case TdApi.Update.UpdateConnectionState { State: TdApi.ConnectionState.ConnectionStateReady }:
            case TdApi.Update.UpdateUser:
                _readyToAuthenticate.Set();
                break;
        }
    }

    private async Task InitializeTdLibAsync()
    {
        var filesLocation = Path.Combine(AppContext.BaseDirectory, "tdlib");
        await _client.ExecuteAsync(new TdApi.SetTdlibParameters
        {
            ApiId = int.Parse(_config.ApiId),
            ApiHash = _config.ApiHash,
            DeviceModel = "PC",
            SystemLanguageCode = "en",
            ApplicationVersion = _config.ApplicationVersion,
            DatabaseDirectory = filesLocation,
            FilesDirectory = filesLocation
        });
    }

    protected TdClient Client => _client;
}