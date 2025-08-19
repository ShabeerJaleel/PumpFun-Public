namespace PumpFun.Core.Options
{
    public class TokenCreationOptions
    {
        public decimal SlippagePercentage { get; set; }
        public decimal PriorityFee { get; set; }
        public bool Simulation { get; set; }
    }
}
