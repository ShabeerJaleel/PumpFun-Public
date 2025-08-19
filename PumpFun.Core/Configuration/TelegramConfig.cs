namespace PumpFun.Core.Configuration;

public class TelegramConfig
{
    public string ApiId { get; set; } = string.Empty;
    public string ApiHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string BotUsername { get; set; } = "BOTNAME";
    public string[] GroupNames { get; set; } = Array.Empty<string>();
}
