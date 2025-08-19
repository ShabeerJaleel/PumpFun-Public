using System.Text.Json;
using PumpFun.Core.Models;

namespace PumpFun.Daemon.Services.PumpPortal
{
    public static class TransactionMessageDeserializer
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static TransactionModel? DeserializeTransactionMessage(string jsonData)
        {
            ArgumentNullException.ThrowIfNull(jsonData);
            return JsonSerializer.Deserialize<TransactionModel>(jsonData, _options);
        }
    }
}
