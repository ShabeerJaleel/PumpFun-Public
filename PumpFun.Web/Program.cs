using Microsoft.EntityFrameworkCore;
using PumpFun.Core.Interfaces;
using PumpFun.Infrastructure.Data;
using PumpFun.Infrastructure.Repositories;
using Microsoft.Extensions.FileProviders;
using PumpFun.Infrastructure.Services;
using PumpFun.Core.Options;
using PumpFun.Infrastructure.Services.PumpPortal;  // Updated namespace
using PumpFun.Web.Hubs;
using PumpFun.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration for wallet options
builder.Services.Configure<WalletOptions>(
    builder.Configuration.GetSection(WalletOptions.ConfigurationSection));

builder.Services.Configure<TokenCreationOptions>(
    builder.Configuration.GetSection("TokenCreation"));

// Add SignalR before AddControllers
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100KB
});

builder.Services.AddControllers();

// Replace both DbContext registrations with pooled factory
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Change from AddScoped to AddTransient to match Daemon configuration
builder.Services.AddTransient<ITokenRepository, TokenRepository>();

// Add these service registrations
builder.Services.AddHttpClient();  // Keep this line as it's a core service
builder.Services.AddSingleton<IIpfsService, IpfsService>();
builder.Services.AddSingleton<ISolPriceService, SolPriceService>();

// Add PumpPortal services
builder.Services.AddSingleton<ITokenCreationService, PumpPortalTokenService>();
builder.Services.AddSingleton<ITokenAnalysisProcessor, TokenAnalysisProcessor>();

// Register the new urgent analysis service
builder.Services.AddSingleton<IUrgentAnalysisService, UrgentAnalysisService>();

// Register TelegramMessageService
builder.Services.AddHostedService<TelegramMessageService>();

var app = builder.Build();

// Initialize SOL price service
using (var scope = app.Services.CreateScope())
{
    var solPriceService = scope.ServiceProvider.GetRequiredService<ISolPriceService>();
    await solPriceService.InitializeAsync();
}

// Configure static files to serve from build directory
var buildPath = Path.Combine(Directory.GetCurrentDirectory(), "build");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(buildPath),
    RequestPath = ""
});

// Add before app.UseRouting()
if (app.Environment.IsDevelopment())
{
    app.UseCors(builder => builder
        .WithOrigins("http://localhost:3000")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
}

app.UseRouting();
app.UseAuthorization();

// API endpoints
app.MapControllers();

// Map the TelegramHub
app.MapHub<TelegramHub>("/hubs/telegram");

// Fallback route
app.MapFallback(context =>
{
    context.Response.Headers["Cache-Control"] = "no-cache";
    return context.Response.SendFileAsync(Path.Combine(buildPath, "index.html"));
});

app.Run();