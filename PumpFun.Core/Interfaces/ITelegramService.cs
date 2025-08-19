using System;

namespace PumpFun.Core.Interfaces;

public interface ITelegramService
{
    Task<bool> LoginAsync();
    Task<bool> IsAuthenticatedAsync();
}
