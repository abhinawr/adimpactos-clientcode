using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AdImpactOs.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Add logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Add Rate Limiter (1000 requests per minute)
        services.AddSingleton(new RateLimiterService(1000, TimeSpan.FromMinutes(1)));

        // When EventHubConnection is not configured, register a local forwarder
        // that posts impressions directly to the Campaign API.
        var eventHubConn = context.Configuration["EventHubConnection"];
        if (string.IsNullOrEmpty(eventHubConn))
        {
            services.AddHttpClient<LocalImpressionForwarder>(client =>
            {
                var campaignApiUrl = context.Configuration["CampaignApiUrl"] ?? "http://localhost:5003";
                client.BaseAddress = new Uri(campaignApiUrl);
            });
        }
    })
    .Build();

host.Run();
