using System.Text.Json;
using Microsoft.Extensions.Logging;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models.Requests;
using PumpFun.Core.Models.Responses;
using System.Diagnostics;
using Path = System.IO.Path;
using Microsoft.Extensions.Options;
using PumpFun.Core.Options;

namespace PumpFun.Infrastructure.Services.PumpPortal
{
    public class PumpPortalTokenService : ITokenCreationService
    {
        private readonly ILogger<PumpPortalTokenService> _logger;
        private readonly IIpfsService _ipfsService;
        private readonly string _scriptsPath;
        private readonly WalletOptions _walletOptions;
        private readonly TokenCreationOptions _tokenCreationOptions;

        public PumpPortalTokenService(
            ILogger<PumpPortalTokenService> logger,
            IIpfsService ipfsService,
            IOptions<WalletOptions> walletOptions,
            IOptions<TokenCreationOptions> tokenCreationOptions)
        {
            _logger = logger;
            _ipfsService = ipfsService;
            _walletOptions = walletOptions.Value;
            _tokenCreationOptions = tokenCreationOptions.Value;
            _scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        }

        public async Task<TokenCreationResponse> CreateTokenAsync(TokenCreationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Download image from URL and save to temp file
                byte[] imageData;
                var tempImagePath = Path.GetTempFileName() + ".png";

                try
                {
                    imageData = await _ipfsService.GetImageDataAsync(request.ImageUrl, cancellationToken);
                    await File.WriteAllBytesAsync(tempImagePath, imageData, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download image from URL: {ImageUrl}", request.ImageUrl);
                    throw new Exception("Failed to download image");
                }

                var args = JsonSerializer.Serialize(new
                {
                    walletPrivateKey = _walletOptions.PrivateKey,
                    publicKey = _walletOptions.Address,
                    name = request.Name,
                    symbol = request.Symbol,
                    description = request.Description,
                    imagePath = tempImagePath,
                    twitter = request.Twitter,
                    telegram = request.Telegram,
                    website = request.Website,
                    amount = request.InitialBuyAmount,
                    slippage = _tokenCreationOptions.SlippagePercentage,  // Use from config
                    priorityFee = _tokenCreationOptions.PriorityFee,      // Use from config
                    simulation = request.IsSimulation ?? _tokenCreationOptions.Simulation  // Use request value if provided, otherwise use config
                });

                var result = await ExecuteNodeScriptAsync("token/create", args, cancellationToken);
                // Cleanup temp file
                if (File.Exists(tempImagePath))
                {
                    try
                    {
                        File.Delete(tempImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp image file: {TempImagePath}", tempImagePath);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create token");
                throw;
            }
        }

        private async Task<TokenCreationResponse> ExecuteNodeScriptAsync(string scriptName, string args, CancellationToken cancellationToken)
        {
            var scriptPath = Path.Combine(_scriptsPath, scriptName + ".js");
            
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Script not found: {scriptPath}");

            var escapedArgs = args.Replace("\"", "\\\"");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"--experimental-modules {scriptPath} \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(2)); // 2 minute timeout

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync(cts.Token);

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("Node script error: {Error}", error);
                    throw new Exception($"Node script failed: {error}");
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Node script failed with exit code: {process.ExitCode}");
                }

                var response = JsonSerializer.Deserialize<TokenCreationResponse>(output.Trim());
                
                if (!response.Success)
                {
                    _logger.LogError("Token creation failed: {Error}", response.Error);
                }

                return response;
            }
            finally
            {
                if (!process.HasExited)
                    process.Kill(true);
                
                process.Dispose();
            }
        }
    }
}
