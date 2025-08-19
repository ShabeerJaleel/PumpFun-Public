using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Collections;
using PumpFun.Core.Models;
using System.Collections.Concurrent;
using System.IO.Pipes;
using PumpFun.Core.Interfaces.Telegram;
using PumpFun.Core.Models.Responses;

namespace PumpFun.Daemon.Services.TokenAnalysis
{
    public class TokenAnalysisService : ITokenAnalysisService, IDisposable
    {
        private const decimal MIN_USD_MARKET_CAP = 10000m;
        private const decimal MAX_USD_MARKET_CAP = 20000m;
        private const int QUEUE_THRESHOLD = 10;
        private const string PIPE_NAME = "SyraxUrgentAnalysis";
        private readonly ILogger<TokenAnalysisService> _logger;
        private readonly ITokenRepository _tokenRepository;
        private readonly ISyraxBotService _syraxBotService;
        private readonly ISolPriceService _solPriceService;
        private readonly PriorityTokenQueue _priorityQueue = new();
        private TaskCompletionSource<TokenAnalysisResponseEventArgs> _currentAnalysis = new();
        private readonly CancellationTokenSource _processingCts = new();
        private readonly ConcurrentQueue<string> _urgentQueue = new();
        private readonly CancellationTokenSource _pipeServerCts = new();

        public TokenAnalysisService(
            ILogger<TokenAnalysisService> logger,
            ITokenRepository tokenRepository,
            ISyraxBotService syraxBotService,
            ISolPriceService solPriceService)
        {
            _logger = logger;
            _tokenRepository = tokenRepository;
            _syraxBotService = syraxBotService;
            _solPriceService = solPriceService;
            _syraxBotService.OnTokenAnalysisReceived += HandleSyraxAnalysis;
            _ = ProcessTokenAnalysisQueueAsync(_processingCts.Token);
            _ = StartPipeServerAsync(_pipeServerCts.Token);
        }

        private decimal CalculateDynamicMarketCapThreshold()
        {
            if (_priorityQueue.Count <= QUEUE_THRESHOLD)
                return MIN_USD_MARKET_CAP;

            // Calculate dynamic threshold based on queue size
            // As queue size grows from QUEUE_THRESHOLD to QUEUE_THRESHOLD*2, 
            // market cap threshold grows from MIN to MAX linearly
            var queueFactor = Math.Min(1.0m, (_priorityQueue.Count - QUEUE_THRESHOLD) / (decimal)QUEUE_THRESHOLD);
            return MIN_USD_MARKET_CAP + (MAX_USD_MARKET_CAP - MIN_USD_MARKET_CAP) * queueFactor;
        }

        public async Task EnqueueTokenForAnalysis(Token token)
        {
            if (!token.AnalysisCompleted)
            {
                var usdMarketCap = await _solPriceService.ConvertSolToUsd(token.MarketCapSol);
                var threshold = CalculateDynamicMarketCapThreshold();

                if (usdMarketCap > threshold)
                {
                    _priorityQueue.Enqueue(token.TokenAddress, token.MarketCapSol);
                    _logger.LogInformation("Token {Symbol} enqueued with market cap {usdMarketCap}. Current queue count: {QueueCount}, Threshold: {Threshold}",
                        token.Symbol, usdMarketCap, _priorityQueue.Count, threshold);
                }
            }
        }

        public void SubmitUrgentAnalysis(string tokenAddress)
        {
            _urgentQueue.Enqueue(tokenAddress);
            _logger.LogInformation("Urgent analysis request received for {TokenAddress}", tokenAddress);
        }

        private async Task ProcessTokenAnalysisQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? tokenAddress = null;

                try
                {
                    // Check urgent queue first
                    if (_urgentQueue.TryDequeue(out tokenAddress))
                    {
                        _logger.LogInformation("Processing urgent analysis for {TokenAddress}", tokenAddress);
                    }
                    // Then check regular queue
                    else if (!_priorityQueue.TryDequeue(out tokenAddress))
                    {
                        await Task.Delay(500, cancellationToken);
                        continue;
                    }


                    var token = await _tokenRepository.GetTokenAsync(tokenAddress);

                    if (token.AnalysisCompleted)
                        continue;

                    _currentAnalysis = new TaskCompletionSource<TokenAnalysisResponseEventArgs>();
                    await _syraxBotService.SubmitTokenAddressAsync(tokenAddress);

                    var result = await _currentAnalysis.Task;

                    if (result.IsError)
                        throw new Exception(result.Analysis);

                    if(_currentAnalysis.Task.Exception != null)
                        throw _currentAnalysis.Task.Exception;

                    _logger.LogInformation("Analysis completed for token {Symbol}, {TokenAddress}: {Error}",
                        token.Symbol, tokenAddress, result.Analysis);

                    await Task.Delay(8000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Analysis timed out for token {TokenAddress}", tokenAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error Analyzing token {TokenAddress}", tokenAddress);
                }
            }
        }

        private async Task StartPipeServerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.In);
                try
                {
                    await pipeServer.WaitForConnectionAsync(cancellationToken);
                    using var reader = new StreamReader(pipeServer);
                    var tokenAddress = await reader.ReadLineAsync();
                    
                    if (!string.IsNullOrEmpty(tokenAddress))
                    {
                        SubmitUrgentAnalysis(tokenAddress);
                    }
                }
                catch (OperationCanceledException) {  }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in pipe server");
                }
            }
        }

        private async void HandleSyraxAnalysis(object? sender, TokenAnalysisResponseEventArgs e)
        {
            try
            {
                if (!e.IsError)
                {
                    await _tokenRepository.UpdateTokenAnalysisAsync(e.TokenAddress, e.FormattedAnalysis);
                }

                _currentAnalysis.TrySetResult(e);
            }
            catch (Exception ex)
            {
                _currentAnalysis.TrySetException(ex);
            }
        }

        public void Dispose()
        {
            _processingCts.Cancel();
            _processingCts.Dispose();
            _pipeServerCts.Cancel();
            _pipeServerCts.Dispose();
        }
    }
}