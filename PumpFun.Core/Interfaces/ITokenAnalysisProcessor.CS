using PumpFun.Core.Models.Dtos;

namespace PumpFun.Core.Interfaces 
{
    public interface ITokenAnalysisProcessor 
    {
        void ProcessAnalysis(TokenDto token);
    }
}