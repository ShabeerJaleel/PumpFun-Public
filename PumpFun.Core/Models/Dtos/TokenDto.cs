namespace PumpFun.Core.Models.Dtos
{
    public class TokenDto
    {
        public string TokenAddress { get; set; } = null!;
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
        public decimal MarketCap { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Analysis { get; set; }
        public bool AnalysisCompleted { get; set; }
        public DateTime? AnalysisTimestamp { get; set; }

        // New analysis fields
        public decimal DevHolds { get; set; }
        public decimal DevBoughtPercentage { get; set; }
        public decimal SnipingPercentage { get; set; }
        public decimal? BuysAtTheSameSecond { get; set; }
        public int TokensCreatedByDev { get; set; }
        public int SameNameTokenCount { get; set; }
        public int? SameWebsiteTokenCount { get; set; }
        public int? SameTelegramTokenCount { get; set; }
        public int? SameTwitterTokenCount { get; set; }
        public int? TimesRelaunchedByDev { get; set; }
        public List<string>? UnprocessedRemarks { get; set; }
    }
}
