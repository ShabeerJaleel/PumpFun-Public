using PumpFun.Core.Models;

namespace PumpFun.Core.Interfaces
{
    public interface ITokenAnalysisService
    {
        Task EnqueueTokenForAnalysis(Token token);
        void SubmitUrgentAnalysis(string tokenAddress);
    }
}