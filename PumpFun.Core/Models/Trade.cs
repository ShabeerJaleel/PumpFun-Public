namespace PumpFun.Core.Models
{
    public class Trade
    {
        public required string Signature { get; set; }  // This is now the primary key
        public required string TokenAddress { get; set; }
        public required string TraderPublicKey { get; set; }
        public required string TxType { get; set; }
        public decimal TokenAmount { get; set; }
        public decimal NewTokenBalance { get; set; }
        public decimal VTokensInBondingCurve { get; set; }
        public decimal VSolInBondingCurve { get; set; }
        public decimal MarketCapSol { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
