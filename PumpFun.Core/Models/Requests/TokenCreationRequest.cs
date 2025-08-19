namespace PumpFun.Core.Models.Requests
{
    public class TokenCreationRequest
    {
        public required string TokenAddress { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
        public string? Description { get; set; }
        public string? Twitter { get; set; }
        public string? Telegram { get; set; }
        public string? Website { get; set; }
        public string? ImageUrl { get; set; }  
        public decimal? InitialBuyAmount { get; set; } = 0;
        public bool? IsSimulation { get; set; }
    }
}
