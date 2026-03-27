using Microsoft.Azure.Cosmos;
using AdImpactOs.Survey.Services;
using AdImpactOs.Survey.Migration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AdImpactOs Survey API", Version = "v1" });
});

// Configure CORS for Demo UI and Dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDemoClients", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5010",
                "http://localhost:5004",
                "http://localhost:5002"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var cosmosEndpoint = builder.Configuration["CosmosDb:Endpoint"] ?? "https://localhost:8081";
var cosmosKey = builder.Configuration["CosmosDb:Key"] ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

builder.Services.AddSingleton(sp =>
{
    return new CosmosClient(cosmosEndpoint, cosmosKey, new CosmosClientOptions
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
    });
});

builder.Services.AddScoped<SurveyService>();
builder.Services.AddSingleton<SurveyTokenService>();
builder.Services.AddScoped<SurveyDbMigration>();

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowDemoClients");

app.UseAuthorization();
app.MapControllers();

// Serve static files for the survey-taking page
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

// Auto-run migration and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var migration = scope.ServiceProvider.GetRequiredService<SurveyDbMigration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Running Survey DB migration on startup...");
        await migration.RunMigrationAsync();
        await migration.SeedSampleDataAsync();
        logger.LogInformation("Survey DB migration and seeding completed on startup");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Survey DB auto-migration failed on startup. Migration can be run manually via POST /api/migration/run");
    }
}

app.Run();
