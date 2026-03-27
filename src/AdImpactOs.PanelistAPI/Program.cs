using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AdImpactOs.PanelistAPI.Services;
using AdImpactOs.PanelistAPI.Migration;

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
                "http://localhost:5010",
                "http://localhost:5004",
                "http://localhost:5001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add Azure AD B2C Authentication only when properly configured
var azureAdB2CSection = builder.Configuration.GetSection("AzureAdB2C");
if (azureAdB2CSection.Exists() && !string.IsNullOrEmpty(azureAdB2CSection["ClientId"]) && azureAdB2CSection["ClientId"] != "your-client-id")
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAdB2CSection);
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => {
            options.RequireHttpsMetadata = false;
        });
}

builder.Services.AddAuthorization();

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", cancellationToken);
    };
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
builder.Services.AddScoped<PanelistService>();
builder.Services.AddScoped<PanelistDbMigration>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowDemoClients");

app.UseHttpsRedirection();
app.UseRateLimiter();

if (!app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

// Auto-run migration and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var migration = scope.ServiceProvider.GetRequiredService<PanelistDbMigration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Running Panelist DB migration on startup...");
        await migration.RunMigrationAsync();
        await migration.SeedSampleDataAsync();
        logger.LogInformation("Panelist DB migration and seeding completed on startup");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Panelist DB auto-migration failed on startup. Migration can be run manually via POST /api/migration/run");
    }
}

app.Run();
