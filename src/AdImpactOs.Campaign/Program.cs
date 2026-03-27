using Microsoft.Azure.Cosmos;
using AdImpactOs.Campaign.Services;
using AdImpactOs.Campaign.Migration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for Demo UI and Dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDemoClients", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5010",  // Demo UI (local)
                "http://localhost:5004",  // Dashboard (local)
                "http://localhost:5003"   // Swagger self
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure Cosmos DB
builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration["CosmosDb:ConnectionString"];
    var endpoint = configuration["CosmosDb:Endpoint"];
    var key = configuration["CosmosDb:Key"];

    var cosmosOptions = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        },
        HttpClientFactory = () => new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        }),
        ConnectionMode = ConnectionMode.Gateway,
        LimitToEndpoint = true,
        RequestTimeout = TimeSpan.FromSeconds(30),
    };

    if (!string.IsNullOrEmpty(connectionString))
    {
        return new CosmosClient(connectionString, cosmosOptions);
    }
    else if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
    {
        return new CosmosClient(endpoint, key, cosmosOptions);
    }
    else
    {
        throw new InvalidOperationException("Cosmos DB connection configuration is missing");
    }
});

// Register services
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<ImpressionService>();
builder.Services.AddScoped<CampaignDbMigration>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowDemoClients");

app.UseAuthorization();
app.MapControllers();

// Auto-run migration and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var migration = scope.ServiceProvider.GetRequiredService<CampaignDbMigration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Running Campaign DB migration on startup...");
        await migration.RunMigrationAsync();
        await migration.SeedSampleDataAsync();
        await migration.SeedImpressionDataAsync();
        logger.LogInformation("Campaign DB migration and seeding completed on startup");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Campaign DB auto-migration failed on startup. Migration can be run manually via POST /api/migration/run");
    }
}

app.Run();
