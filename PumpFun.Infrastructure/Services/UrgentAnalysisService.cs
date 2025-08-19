using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces;

namespace PumpFun.Infrastructure.Services
{
    public class UrgentAnalysisService : IUrgentAnalysisService
    {
        private const string PIPE_NAME = "SyraxUrgentAnalysis";
        private readonly ILogger<UrgentAnalysisService> _logger;

        public UrgentAnalysisService(
            ILogger<UrgentAnalysisService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SubmitAnalysisAsync(string tokenAddress)
        {
            using var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out);
            await pipeClient.ConnectAsync(5000); // 5 second timeout
            using var writer = new StreamWriter(pipeClient);
            await writer.WriteLineAsync(tokenAddress);
            await writer.FlushAsync();
            return true;
        }
    }
}
