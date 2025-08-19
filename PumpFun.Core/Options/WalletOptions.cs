namespace PumpFun.Core.Options;

public class WalletOptions
{
    public const string ConfigurationSection = "Wallet";
    public string Address { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
