using System.ComponentModel.DataAnnotations;

namespace PumpFun.Core.Models.Requests
{
    public class TokenFilterRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int? PageNumber { get; set; }

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int? PageSize { get; set; }

        [Range(1, 1000, ErrorMessage = "Limit must be between 1 and 1000")]
        public int Limit { get; set; } = 100;

        [Range(0, double.MaxValue, ErrorMessage = "Minimum market cap must be non-negative")]
        public decimal? MarketCapMin { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Maximum market cap must be non-negative")]
        public decimal? MarketCapMax { get; set; }

        // Add SOL market cap fields
        public decimal? MarketCapMinSol { get; set; }
        public decimal? MarketCapMaxSol { get; set; }

        public bool? AnalysisCompleted { get; set; }

        public DateTime? CreatedAfter { get; set; }
    }
}
