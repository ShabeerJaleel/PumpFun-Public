namespace PumpFun.Daemon
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Channels;
    using PumpFun.Core.Models;
    using PumpFun.Core.Interfaces;
    using PumpFun.Daemon.Services.PumpPortal;
    using PumpFun.Infrastructure.Data; // Added namespace for ApplicationDbContext
    using Microsoft.EntityFrameworkCore;
    using PumpFun.Infrastructure.Repositories;
    using PumpFun.Daemon.Services;
    using PumpFun.Core.Configuration;
    using PumpFun.Daemon.Services.Telegram;
    using Microsoft.Extensions.Options;
    using PumpFun.Daemon.Services.TokenAnalysis;
    using PumpFun.Infrastructure.Services;
    using PumpFun.Core.Interfaces.Telegram;

    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Add Telegram Configuration
                    services.Configure<TelegramConfig>(
                        context.Configuration.GetSection("Telegram"));

                    // Register TelegramConfig as a singleton
                    services.AddSingleton<TelegramConfig>(sp =>
                        sp.GetRequiredService<IOptions<TelegramConfig>>().Value);

                    // Replace DbContext registration with DbContextFactory
                    services.AddDbContextFactory<ApplicationDbContext>(options =>
                        options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

                    // Change repository registrations to transient
                    services.AddTransient<ITokenRepository, TokenRepository>();
                    services.AddTransient<ITradeRepository, TradeRepository>();

                    // Comment out old Telegram service registration
                    // services.AddSingleton<IPumpFunAnalysisService, PumpFunAnalysisService>();

                    // Add new WTelegram-based services
                    services.AddSingleton<ISyraxBotService, SyraxBotService>();
                    services.AddSingleton<ITelegramGroupsService, TelegramGroupsService>();
                    services.AddSingleton<ITokenAnalysisService, TokenAnalysisService>();
                    services.AddSingleton<TelegramBroadcastService>();  // Add this line

                    services.AddHttpClient();
                    services.AddSingleton<IWebSocketClient, PumpPortalWebSocketClient>();
                    services.AddSingleton<ITokenProcessingService, TokenProcessingService>();
                    services.AddSingleton<ITradeProcessingService, TradeProcessingService>();
                    services.AddSingleton<IIpfsService, IpfsService>();
                    services.AddSingleton<TokenCreationChannel>();
                    services.AddSingleton<TradeChannel>();
                    services.AddHostedService<DaemonService>();

                    // Add SOL price services
                    services.AddSingleton<ISolPriceService, SolPriceService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug); // Ensure debug logs are captured
                });
    }
}
