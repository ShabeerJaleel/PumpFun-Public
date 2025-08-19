using PumpFun.Core.Interfaces;
using PumpFun.Core.Models.Dtos;
using System.Text.RegularExpressions;
using TL;
using WTelegram;

namespace PumpFun.Infrastructure.Services 
{
    public class TokenAnalysisProcessor : ITokenAnalysisProcessor 
    {
        private static readonly Dictionary<string, Regex> Patterns = new()
        {
            { "Section", new Regex(@"(?<=Comments:.*?\r?\n)(.*?)(?=Trade.*?via)", RegexOptions.Compiled | RegexOptions.Singleline) },
            { "DevHolds", new Regex(@"Dev Holds: (?:Nothing|0|(?:[\d.]+[KMB]? \()?(\d+(?:\.\d+)?)%)", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "DevBought", new Regex(@"Dev did not buy their own token", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "DevTokens", new Regex(@"Dev (?:created (\d+)(?: other)? tokens|only created this token)", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "SameNames", new Regex(@"(\d+) tokens with the same name", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "SameWebsites", new Regex(@"(\d+) tokens with the same website", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "SameTelegrams", new Regex(@"(\d+) tokens with the same Telegram", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "SameTwitters", new Regex(@"(\d+) tokens with the same Twitter", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "Relaunches", new Regex(@"Dev launched this project (\d+) time\(s\) before", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "DevBoughtPercent", new Regex(@"Dev bought his own token with.*?(\d+(?:\.\d+)?)%", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "BuysAtTheSameSecond", new Regex(@"(\d+) buys \(of 0\.1\+ SOL\) are in the same second.*?(\d+(?:\.\d+)?)%", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
            { "Sniping", new Regex(@"Sniped! \d+ trades in the same second as coin creation, (\d+(?:\.\d+)?)%", RegexOptions.Compiled | RegexOptions.IgnoreCase) }
        };

        public void ProcessAnalysis(TokenDto token)
        {
            if (string.IsNullOrWhiteSpace(token.Analysis))
                return;

            string strippedText = token.Analysis;
            HtmlText.HtmlToEntities(null, ref strippedText);

            var sectionMatch = Patterns["Section"].Match(strippedText);
            if (!sectionMatch.Success)
                return;

            var relevantSection = sectionMatch.Value;
            var unprocessedRemarks = new List<string>();

            foreach (var line in relevantSection.Split('\n'))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || 
                    trimmedLine.Contains("Special Remarks:", StringComparison.OrdinalIgnoreCase)) 
                    continue;

                // Simpler check for Twitter/Website/Telegram line
                var words = new HashSet<string> { "Twitter", "Website", "Telegram" };
                if (trimmedLine.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries)
                            .All(w => words.Contains(w)))
                    continue;

                var processed = false;
                foreach (var pattern in Patterns.Where(p => p.Key != "Section"))
                {
                    var match = pattern.Value.Match(trimmedLine);
                    if (!match.Success) continue;

                    processed = true;
                    switch (pattern.Key)
                    {
                        case "DevHolds":
                            if (match.Groups[1].Success)
                            {
                                // Percentage value found in parentheses
                                token.DevHolds = decimal.Parse(match.Groups[1].Value);
                            }
                            else
                            {
                                // Handle "Nothing" or "0" cases
                                var value = match.Value.ToLowerInvariant();
                                token.DevHolds = value.Contains("nothing") || value.EndsWith("0") ? 0 : 0;
                            }
                            break;
                        case "DevBought":
                            token.DevBoughtPercentage = 0;
                            break;
                        case "DevBoughtPercent":
                            token.DevBoughtPercentage = decimal.Parse(match.Groups[1].Value);
                            break;
                        case "DevTokens":
                            token.TokensCreatedByDev = match.Groups[1].Success ? 
                                int.Parse(match.Groups[1].Value) : 1;
                            break;
                        case "SameNames":
                            token.SameNameTokenCount = int.Parse(match.Groups[1].Value);
                            break;
                        case "SameWebsites":
                            token.SameWebsiteTokenCount = int.Parse(match.Groups[1].Value);
                            break;
                        case "SameTelegrams":
                            token.SameTelegramTokenCount = int.Parse(match.Groups[1].Value);
                            break;
                        case "SameTwitters":
                            token.SameTwitterTokenCount = int.Parse(match.Groups[1].Value);
                            break;
                        case "Relaunches":
                            token.TimesRelaunchedByDev = int.Parse(match.Groups[1].Value);
                            break;
                        case "BuysAtTheSameSecond":
                            token.BuysAtTheSameSecond = decimal.Parse(match.Groups[2].Value);
                            break;
                        case "Sniping":
                            token.SnipingPercentage = decimal.Parse(match.Groups[1].Value);
                            break;
                    }
                }

                if (!processed)
                {
                    unprocessedRemarks.Add(trimmedLine);
                }
            }

            token.UnprocessedRemarks = unprocessedRemarks;
        }
    }
}