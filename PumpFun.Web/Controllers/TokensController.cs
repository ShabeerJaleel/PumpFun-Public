using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PumpFun.Core.Interfaces;
using PumpFun.Core.Models;
using PumpFun.Core.Models.Requests;
using PumpFun.Core.Extensions;
using PumpFun.Core.Models.Dtos;
using PumpFun.Core.Options;

namespace PumpFun.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokensController : ControllerBase
    {
        private readonly ITokenRepository _tokenRepository;
        private readonly ISolPriceService _solPriceService;
        private readonly ITokenCreationService _tokenCreationService;
        private readonly IIpfsService _ipfsService;
        private readonly ITokenAnalysisProcessor _tokenAnalysisProcessor;
        private readonly IUrgentAnalysisService _urgentAnalysisService;

        public TokensController(
            ITokenRepository tokenRepository,
            ISolPriceService solPriceService,
            ITokenCreationService tokenCreationService,
            IIpfsService ipfsService,
            ITokenAnalysisProcessor tokenAnalysisProcessor,
            IUrgentAnalysisService urgentAnalysisService)
        {
            _tokenRepository = tokenRepository;
            _solPriceService = solPriceService;
            _tokenCreationService = tokenCreationService;
            _ipfsService = ipfsService;
            _tokenAnalysisProcessor = tokenAnalysisProcessor;
            _urgentAnalysisService = urgentAnalysisService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTokensAsync([FromQuery] TokenFilterRequest filter)
        {
            if (filter.MarketCapMin.HasValue)
            {
                filter.MarketCapMinSol = await _solPriceService.ConvertUsdToSol(filter.MarketCapMin.Value);
            }

            if (filter.MarketCapMax.HasValue)
            {
                filter.MarketCapMaxSol = await _solPriceService.ConvertUsdToSol(filter.MarketCapMax.Value);
            }
            
            var tokens = await _tokenRepository.GetTokensAsync(filter);
            
            // Convert tokens to DTOs with USD market cap
            var tokenDtos = new List<TokenDto>();
            foreach (var token in tokens)
            {
                var marketCapUsd = await _solPriceService.ConvertSolToUsd(token.MarketCapSol);
                var tokenDto = token.ToDto(marketCapUsd);
                
                // Process analysis if it exists
                if (!string.IsNullOrWhiteSpace(tokenDto.Analysis))
                {
                    _tokenAnalysisProcessor.ProcessAnalysis(tokenDto);
                }
                
                tokenDtos.Add(tokenDto);
            }
            
            return Ok(tokenDtos);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAsync([FromBody] TokenCreationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Get existing token data
                var existingToken = await _tokenRepository.GetTokenAsync(request.TokenAddress);
                
                // Fill in missing fields from existing token
                if (existingToken != null)
                {
                    request.Name ??= existingToken.Name;
                    request.Symbol ??= existingToken.Symbol;
                    request.Description ??= existingToken.Description;
                    request.Twitter ??= existingToken.Twitter;
                    request.Telegram ??= existingToken.Telegram;
                    request.Website ??= existingToken.Website;
                }

                // Handle image URL
                if (string.IsNullOrEmpty(request.ImageUrl))
                {
                    if (existingToken == null)
                    {
                        return BadRequest(new { Error = "Image URL is required for new tokens" });
                    }
                    request.ImageUrl = existingToken.Image;
                }

                var result = await _tokenCreationService.CreateTokenAsync(request, cancellationToken);
                
                return Ok(new
                {
                    success = result.Success,
                    tokenAddress = result.Mint,
                    explorerUrl = result.Explorer,
                    error = result.Success ? null : result.Error,
                    isSimulation = result.IsSimulation
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("analyse")]
        public IActionResult Analyse([FromBody] string tokenAddress)
        {
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                return BadRequest(new { error = "Token address is required" });
            }

            try
            {
                // Fire and forget
                _ = _urgentAnalysisService.SubmitAnalysisAsync(tokenAddress);
                return Ok(new { submitted = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}