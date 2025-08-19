using System;

namespace PumpFun.Core.Models
{
    public class Token
    {
        public string TokenAddress { get; set; } = null!;  // Primary Key
        public string Symbol { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public string? Twitter { get; set; }
        public string? Telegram { get; set; }
        public string? Website { get; set; }
        public decimal InitialBuy { get; set; }
        public decimal VTokens { get; set; }
        public decimal VSol { get; set; }
        public decimal MarketCapSol { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Analysis { get; set; }
        public bool AnalysisCompleted { get; set; } = false;
        public DateTime? AnalysisTimestamp { get; set; }

        // Future fields can be added here
    }
}