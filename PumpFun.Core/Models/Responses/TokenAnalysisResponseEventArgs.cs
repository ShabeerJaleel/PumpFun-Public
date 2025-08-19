namespace PumpFun.Core.Models.Responses
{
public class TokenAnalysisResponseEventArgs : EventArgs
{
    private string _tokenAddress = string.Empty;
    private string _analysis = string.Empty;
    private string _formattedAnalysis = string.Empty;

    public TokenAnalysisResponseEventArgs()
    {
        Timestamp = DateTime.UtcNow;
    }

    public string TokenAddress 
    { 
        get => _tokenAddress;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Token address cannot be empty", nameof(value));
            _tokenAddress = value;
        }
    }

    public string Analysis 
    { 
        get => _analysis;
        set => _analysis = value ?? string.Empty;
    }

    public string FormattedAnalysis
    {
        get => _formattedAnalysis;
        set => _formattedAnalysis = value ?? string.Empty;
    }

    public bool IsError { get; set; }
    public DateTime Timestamp { get; set; }
}
}