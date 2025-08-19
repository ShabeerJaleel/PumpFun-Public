using System.Text.Json.Serialization;

namespace PumpFun.Core.Models
{
    public class TokenInfo
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("symbol")]
        public required string Symbol { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("showName")]
        public bool ShowName { get; set; }

        [JsonPropertyName("twitter")]
        public string? Twitter { get; set; }

        [JsonPropertyName("telegram")]
        public string? Telegram { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }
    }
}
