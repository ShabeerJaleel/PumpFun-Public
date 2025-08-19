namespace PumpFun.Core.Interfaces
{
    public interface ISolPriceService
    {
        Task InitializeAsync();
        Task<decimal> GetCurrentSolPriceAsync();
        Task<decimal> ConvertSolToUsd(decimal solAmount);
        Task<decimal> ConvertUsdToSol(decimal usdAmount);
    }
}