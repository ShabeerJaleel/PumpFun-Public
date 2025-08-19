using System.Text.Json.Serialization;
using PumpFun.Core.Models;

namespace PumpFun.Core.Models
{
    public class TransactionModel
    {
        public required string Signature { get; set; }
        public required string Mint { get; set; }
        public required string TraderPublicKey { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TransactionType TxType { get; set; }
        
        public decimal? TokenAmount { get; set; }
        public decimal? NewTokenBalance { get; set; }
        public decimal? InitialBuy { get; set; }
        public required string BondingCurveKey { get; set; }
        public decimal VTokensInBondingCurve { get; set; }
        public decimal VSolInBondingCurve { get; set; }
        public decimal MarketCapSol { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
        public string? Uri { get; set; }
    }
}