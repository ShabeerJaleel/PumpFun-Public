using System.Text.Json.Serialization;

namespace PumpFun.Core.Models.Responses
{
    public class TokenCreationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("mint")]
        public string Mint { get; set; }

        [JsonPropertyName("computeUnits")]
        public int? ComputeUnits { get; set; }

        [JsonPropertyName("logs")]
        public string[] Logs { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("error")]
        public object Error { get; set; }  // Change to object to handle both string and ErrorInfo

        [JsonIgnore]
        public bool IsSimulation => Type == "simulation";

        [JsonIgnore]
        public string ErrorMessage => Error switch
        {
            string str => str,
            ErrorInfo info => info.ToString(),
            _ => null
        };

        [JsonPropertyName("explorer")]
        public string Explorer { get; set; }
    }

    public class ErrorInfo
    {
        [JsonPropertyName("InstructionError")]
        public object[] InstructionError { get; set; }

        public override string ToString()
        {
            if (InstructionError == null || InstructionError.Length < 2)
                return "Unknown error";

            return System.Text.Json.JsonSerializer.Serialize(InstructionError[1]);
        }
    }
}
