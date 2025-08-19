namespace PumpFun.Core.Interfaces
{
    public interface IUrgentAnalysisService
    {
        Task<bool> SubmitAnalysisAsync(string tokenAddress);
    }
}
