using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AdImpactOs.EventConsumer;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConfiguration(configuration.GetSection("Logging"))
        .AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("AdImpactOs EventHub Consumer starting...");

try
{
    var processor = new EventProcessor(
        loggerFactory.CreateLogger<EventProcessor>(),
        configuration);

    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (s, e) =>
    {
        logger.LogInformation("Cancellation requested");
        cts.Cancel();
        e.Cancel = true;
    };

    await processor.StartAsync(cts.Token);

    logger.LogInformation("EventHub consumer is running. Press Ctrl+C to exit.");

    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Consumer stopped gracefully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error in EventHub consumer");
    return 1;
}

return 0;
