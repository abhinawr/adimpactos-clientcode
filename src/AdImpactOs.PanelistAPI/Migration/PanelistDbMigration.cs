using Microsoft.Azure.Cosmos;
using System.Collections.ObjectModel;

namespace AdImpactOs.PanelistAPI.Migration;

/// <summary>
/// Migration script to create Cosmos DB database, container, and indexes for Panelists
/// </summary>
public class PanelistDbMigration
{
    private readonly CosmosClient _cosmosClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PanelistDbMigration> _logger;

    public PanelistDbMigration(CosmosClient cosmosClient, IConfiguration configuration, ILogger<PanelistDbMigration> logger)
    {
        _cosmosClient = cosmosClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Run the migration to create database and container
    /// </summary>
    public async Task RunMigrationAsync()
    {
        var databaseName = _configuration["CosmosDb:DatabaseName"] ?? "AdTrackingDB";
        var containerName = _configuration["CosmosDb:ContainerName"] ?? "Panelists";

        try
        {
            // Create database if not exists
            _logger.LogInformation("Creating database: {DatabaseName}", databaseName);
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                databaseName,
                throughput: 400 // RU/s
            );

            if (databaseResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Database created: {DatabaseName}", databaseName);
            }
            else
            {
                _logger.LogInformation("Database already exists: {DatabaseName}", databaseName);
            }

            var database = _cosmosClient.GetDatabase(databaseName);

            // Define container properties
            var containerProperties = new ContainerProperties
            {
                Id = containerName,
                PartitionKeyPath = "/id",
                IndexingPolicy = new IndexingPolicy
                {
                    Automatic = true,
                    IndexingMode = IndexingMode.Consistent,
                    IncludedPaths =
                    {
                        new IncludedPath { Path = "/*" }
                    },
                    ExcludedPaths =
                    {
                        new ExcludedPath { Path = "/\"_etag\"/?" }
                    },
                    CompositeIndexes =
                    {
                        new Collection<CompositePath>
                        {
                            new CompositePath { Path = "/consentGiven", Order = CompositePathSortOrder.Ascending },
                            new CompositePath { Path = "/isActive", Order = CompositePathSortOrder.Ascending }
                        },
                        new Collection<CompositePath>
                        {
                            new CompositePath { Path = "/country", Order = CompositePathSortOrder.Ascending },
                            new CompositePath { Path = "/createdAt", Order = CompositePathSortOrder.Descending }
                        }
                    }
                }
            };

            // Create container if not exists
            _logger.LogInformation("Creating container: {ContainerName}", containerName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties);

            if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Container created: {ContainerName}", containerName);
            }
            else
            {
                _logger.LogInformation("Container already exists: {ContainerName}", containerName);
            }

            _logger.LogInformation("Panelist migration completed successfully");
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Panelist migration failed: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Seed sample panelist data for testing
    /// </summary>
    public async Task SeedSampleDataAsync()
    {
        var databaseName = _configuration["CosmosDb:DatabaseName"] ?? "AdTrackingDB";
        var containerName = _configuration["CosmosDb:ContainerName"] ?? "Panelists";
        var container = _cosmosClient.GetDatabase(databaseName).GetContainer(containerName);

        var samplePanelists = GetSamplePanelists();

        foreach (var panelist in samplePanelists)
        {
            try
            {
                dynamic p = panelist;
                string panelistId = p.id;
                await container.CreateItemAsync(panelist, new PartitionKey(panelistId));
                _logger.LogInformation("Seeded panelist: {PanelistId}", panelistId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                dynamic p = panelist;
                string panelistId = p.id;
                _logger.LogInformation("Panelist already exists: {PanelistId}", panelistId);
            }
        }

        _logger.LogInformation("Sample panelist data seeding completed - {Count} panelists seeded", samplePanelists.Count);
    }

    private List<object> GetSamplePanelists()
    {
        // Original 20 panelists
        var panelists = new List<object>
        {
            // Young adults - Tech-savvy, active on social media
            new
            {
                id = "panelist-001",
                panelistId = "panelist-001",
                email = "nupurabhi1@gmail.com",
                firstName = "Emma",
                lastName = "Johnson",
                age = 22,
                ageRange = "18-24",
                gender = "F",
                hhIncomeBucket = "25k-50k",
                interests = "technology,gaming,music,fitness",
                country = "US",
                postalCode = "10001",
                deviceType = "Mobile",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-45),
                lastActive = DateTime.UtcNow.AddDays(-1),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-45),
                updatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new
            {
                id = "panelist-002",
                panelistId = "panelist-002",
                email = "nupurabhi1@gmail.com",
                firstName = "Liam",
                lastName = "Martinez",
                age = 24,
                ageRange = "18-24",
                gender = "M",
                hhIncomeBucket = "50k-75k",
                interests = "sports,fitness,automotive,travel",
                country = "US",
                postalCode = "90210",
                deviceType = "Desktop",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-60),
                lastActive = DateTime.UtcNow.AddDays(-2),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-60),
                updatedAt = DateTime.UtcNow.AddDays(-10)
            },
            
            // Young professionals - Career-focused
            new
            {
                id = "panelist-003",
                panelistId = "panelist-003",
                email = "nupurabhi1@gmail.com",
                firstName = "Sophia",
                lastName = "Chen",
                age = 28,
                ageRange = "25-34",
                gender = "F",
                hhIncomeBucket = "75k-100k",
                interests = "finance,travel,shopping,food",
                country = "CA",
                postalCode = "M5H 2N2",
                deviceType = "Mobile",
                browser = "Safari",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-30),
                lastActive = DateTime.UtcNow.AddDays(-1),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-30),
                updatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new
            {
                id = "panelist-004",
                panelistId = "panelist-004",
                email = "nupurabhi1@gmail.com",
                firstName = "Noah",
                lastName = "Williams",
                age = 31,
                ageRange = "25-34",
                gender = "M",
                hhIncomeBucket = "100k-150k",
                interests = "technology,finance,outdoor,fitness",
                country = "UK",
                postalCode = "SW1A 1AA",
                deviceType = "Desktop",
                browser = "Firefox",
                consentGdpr = true,
                consentCcpa = false,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-90),
                lastActive = DateTime.UtcNow.AddDays(-3),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-90),
                updatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new
            {
                id = "panelist-005",
                panelistId = "panelist-005",
                email = "nupurabhi1@gmail.com",
                firstName = "Olivia",
                lastName = "Brown",
                age = 29,
                ageRange = "25-34",
                gender = "F",
                hhIncomeBucket = "75k-100k",
                interests = "health,wellness,fitness,travel,luxury",
                country = "AU",
                postalCode = "2000",
                deviceType = "Tablet",
                browser = "Safari",
                consentGdpr = true,
                consentCcpa = false,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-20),
                lastActive = DateTime.UtcNow.AddDays(-1),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-20),
                updatedAt = DateTime.UtcNow.AddDays(-2)
            },
            
            // Established professionals - Family-oriented
            new
            {
                id = "panelist-006",
                panelistId = "panelist-006",
                email = "nupurabhi1@gmail.com",
                firstName = "James",
                lastName = "Anderson",
                age = 36,
                ageRange = "35-44",
                gender = "M",
                hhIncomeBucket = "100k-150k",
                interests = "family,home,automotive,finance,shopping",
                country = "US",
                postalCode = "60601",
                deviceType = "Desktop",
                browser = "Edge",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-120),
                lastActive = DateTime.UtcNow.AddDays(-1),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-120),
                updatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new
            {
                id = "panelist-007",
                panelistId = "panelist-007",
                email = "nupurabhi1@gmail.com",
                firstName = "Isabella",
                lastName = "Garcia",
                age = 38,
                ageRange = "35-44",
                gender = "F",
                hhIncomeBucket = "> 150k",
                interests = "shopping,family,education,health,food",
                country = "US",
                postalCode = "02108",
                deviceType = "Mobile",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-75),
                lastActive = DateTime.UtcNow.AddDays(-1),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-75),
                updatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new
            {
                id = "panelist-008",
                panelistId = "panelist-008",
                email = "nupurabhi1@gmail.com",
                firstName = "Ethan",
                lastName = "Taylor",
                age = 42,
                ageRange = "35-44",
                gender = "M",
                hhIncomeBucket = "> 150k",
                interests = "automotive,outdoor,family,finance,technology",
                country = "CA",
                postalCode = "V6B 1A1",
                deviceType = "Desktop",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-50),
                lastActive = DateTime.UtcNow.AddDays(-2),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-50),
                updatedAt = DateTime.UtcNow.AddDays(-7)
            },
            
            // Mid-career professionals
            new
            {
                id = "panelist-009",
                panelistId = "panelist-009",
                email = "nupurabhi1@gmail.com",
                firstName = "Ava",
                lastName = "Rodriguez",
                age = 47,
                ageRange = "45-54",
                gender = "F",
                hhIncomeBucket = "100k-150k",
                interests = "health,wellness,luxury,travel,culture",
                country = "US",
                postalCode = "30301",
                deviceType = "Tablet",
                browser = "Safari",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-100),
                lastActive = DateTime.UtcNow.AddDays(-5),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-100),
                updatedAt = DateTime.UtcNow.AddDays(-12)
            },
            new
            {
                id = "panelist-010",
                panelistId = "panelist-010",
                email = "nupurabhi1@gmail.com",
                firstName = "William",
                lastName = "Lee",
                age = 51,
                ageRange = "45-54",
                gender = "M",
                hhIncomeBucket = "> 150k",
                interests = "finance,travel,automotive,technology,culture",
                country = "UK",
                postalCode = "E1 6AN",
                deviceType = "Desktop",
                browser = "Firefox",
                consentGdpr = true,
                consentCcpa = false,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-150),
                lastActive = DateTime.UtcNow.AddDays(-7),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-150),
                updatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new
            {
                id = "panelist-011",
                panelistId = "panelist-011",
                email = "nupurabhi1@gmail.com",
                firstName = "Mia",
                lastName = "Davis",
                age = 49,
                ageRange = "45-54",
                gender = "F",
                hhIncomeBucket = "75k-100k",
                interests = "shopping,family,health,travel,home",
                country = "AU",
                postalCode = "3000",
                deviceType = "Mobile",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = false,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-80),
                lastActive = DateTime.UtcNow.AddDays(-3),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-80),
                updatedAt = DateTime.UtcNow.AddDays(-8)
            },
            
            // Pre-retirement professionals
            new
            {
                id = "panelist-012",
                panelistId = "panelist-012",
                email = "nupurabhi1@gmail.com",
                firstName = "Benjamin",
                lastName = "Wilson",
                age = 57,
                ageRange = "55-64",
                gender = "M",
                hhIncomeBucket = "> 150k",
                interests = "finance,travel,luxury,health,culture",
                country = "US",
                postalCode = "33101",
                deviceType = "Desktop",
                browser = "Edge",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-200),
                lastActive = DateTime.UtcNow.AddDays(-10),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-200),
                updatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new
            {
                id = "panelist-013",
                panelistId = "panelist-013",
                email = "nupurabhi1@gmail.com",
                firstName = "Charlotte",
                lastName = "Moore",
                age = 59,
                ageRange = "55-64",
                gender = "F",
                hhIncomeBucket = "100k-150k",
                interests = "travel,culture,wellness,shopping,luxury",
                country = "CA",
                postalCode = "T2P 1J9",
                deviceType = "Tablet",
                browser = "Safari",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-110),
                lastActive = DateTime.UtcNow.AddDays(-2),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-110),
                updatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new
            {
                id = "panelist-014",
                panelistId = "panelist-014",
                email = "nupurabhi1@gmail.com",
                firstName = "Lucas",
                lastName = "Martinez",
                age = 62,
                ageRange = "55-64",
                gender = "M",
                hhIncomeBucket = "> 150k",
                interests = "technology,finance,travel,health,outdoor",
                country = "US",
                postalCode = "94102",
                deviceType = "Desktop",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-180),
                lastActive = DateTime.UtcNow.AddDays(-5),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-180),
                updatedAt = DateTime.UtcNow.AddDays(-15)
            },
            
            // Retirees
            new
            {
                id = "panelist-015",
                panelistId = "panelist-015",
                email = "nupurabhi1@gmail.com",
                firstName = "Amelia",
                lastName = "Thomas",
                age = 67,
                ageRange = "65+",
                gender = "F",
                hhIncomeBucket = "75k-100k",
                interests = "travel,culture,health,luxury,family",
                country = "UK",
                postalCode = "M1 1AE",
                deviceType = "Desktop",
                browser = "Firefox",
                consentGdpr = true,
                consentCcpa = false,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-250),
                lastActive = DateTime.UtcNow.AddDays(-14),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-250),
                updatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new
            {
                id = "panelist-016",
                panelistId = "panelist-016",
                email = "nupurabhi1@gmail.com",
                firstName = "Alexander",
                lastName = "Jackson",
                age = 69,
                ageRange = "65+",
                gender = "M",
                hhIncomeBucket = "100k-150k",
                interests = "travel,culture,health,finance,outdoor",
                country = "AU",
                postalCode = "4000",
                deviceType = "Tablet",
                browser = "Safari",
                consentGdpr = true,
                consentCcpa = false,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-300),
                lastActive = DateTime.UtcNow.AddDays(-20),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-300),
                updatedAt = DateTime.UtcNow.AddDays(-40)
            },
            
            // Diverse backgrounds
            new
            {
                id = "panelist-017",
                panelistId = "panelist-017",
                email = "nupurabhi1@gmail.com",
                firstName = "Priya",
                lastName = "Patel",
                age = 33,
                ageRange = "25-34",
                gender = "F",
                hhIncomeBucket = "100k-150k",
                interests = "technology,health,food,culture,shopping",
                country = "US",
                postalCode = "98101",
                deviceType = "Mobile",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-40),
                lastActive = DateTime.UtcNow.AddDays(-1),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-40),
                updatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new
            {
                id = "panelist-018",
                panelistId = "panelist-018",
                email = "nupurabhi1@gmail.com",
                firstName = "Muhammad",
                lastName = "Ahmed",
                age = 45,
                ageRange = "45-54",
                gender = "M",
                hhIncomeBucket = "> 150k",
                interests = "finance,technology,family,culture,travel",
                country = "UK",
                postalCode = "B1 1AA",
                deviceType = "Desktop",
                browser = "Edge",
                consentGdpr = true,
                consentCcpa = false,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-130),
                lastActive = DateTime.UtcNow.AddDays(-4),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-130),
                updatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new
            {
                id = "panelist-019",
                panelistId = "panelist-019",
                email = "nupurabhi1@gmail.com",
                firstName = "Elena",
                lastName = "Gonzalez",
                age = 26,
                ageRange = "25-34",
                gender = "F",
                hhIncomeBucket = "50k-75k",
                interests = "fashion,music,food,shopping,travel",
                country = "US",
                postalCode = "75201",
                deviceType = "Mobile",
                browser = "Safari",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-25),
                lastActive = DateTime.UtcNow.AddDays(-1),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-25),
                updatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new
            {
                id = "panelist-020",
                panelistId = "panelist-020",
                email = "nupurabhi1@gmail.com",
                firstName = "Kenji",
                lastName = "Tanaka",
                age = 54,
                ageRange = "45-54",
                gender = "M",
                hhIncomeBucket = "> 150k",
                interests = "technology,finance,automotive,culture,travel",
                country = "CA",
                postalCode = "K1A 0A9",
                deviceType = "Desktop",
                browser = "Chrome",
                consentGdpr = true,
                consentCcpa = true,
                consentGiven = true,
                consentTimestamp = DateTime.UtcNow.AddDays(-160),
                lastActive = DateTime.UtcNow.AddDays(-8),
                pointsBalance = 0,
                isActive = true,
                createdAt = DateTime.UtcNow.AddDays(-160),
                updatedAt = DateTime.UtcNow.AddDays(-18)
            }
        };

        // Generate 80 additional panelists (panelist-021 through panelist-100)
        var firstNames = new[]
        {
            "Aiden","Harper","Jackson","Riley","Carter","Zoey","Mason","Chloe",
            "Logan","Lily","Sebastian","Grace","Mateo","Nora","Owen","Ella",
            "Daniel","Mila","Henry","Aria","Jack","Scarlett","Leo","Layla",
            "Gabriel","Penelope","Samuel","Victoria","David","Hannah",
            "Caleb","Aubrey","Ryan","Stella","Nathan","Maya","Isaac","Violet",
            "Luke","Paisley","Wyatt","Savannah","Julian","Claire","Grayson","Brooklyn",
            "Jaxon","Bella","Levi","Aurora","Finn","Hazel","Elijah","Nova",
            "Kai","Luna","Oliver","Eliana","Theo","Jade","Ezra","Ivy",
            "Axel","Athena","Miles","Willow","Asher","Emery","Xavier","Piper",
            "Dominic","Taylor","Adrian","Morgan","Tristan","Quinn","Felix","Sage"
        };
        var lastNames = new[]
        {
            "Smith","Jones","Brown","Davis","Miller","Wilson","Moore","Taylor",
            "Anderson","Thomas","Jackson","White","Harris","Martin","Thompson","Garcia",
            "Robinson","Clark","Lewis","Lee","Walker","Hall","Allen","Young",
            "Hernandez","King","Wright","Lopez","Hill","Scott","Green","Adams",
            "Baker","Gonzalez","Nelson","Carter","Mitchell","Perez","Roberts","Turner",
            "Phillips","Campbell","Parker","Evans","Edwards","Collins","Stewart","Sanchez",
            "Morris","Rogers","Reed","Cook","Morgan","Bell","Murphy","Bailey",
            "Rivera","Cooper","Richardson","Cox","Howard","Ward","Torres","Peterson",
            "Gray","Ramirez","James","Watson","Brooks","Kelly","Sanders","Price",
            "Bennett","Wood","Barnes","Ross","Henderson","Coleman","Jenkins","Perry"
        };
        var ageRanges = new[] { "18-24", "25-34", "35-44", "45-54", "55-64", "65+" };
        var ageRangeMins = new[] { 18, 25, 35, 45, 55, 65 };
        var ageRangeMaxs = new[] { 24, 34, 44, 54, 64, 75 };
        var genders = new[] { "M", "F" };
        var incomeBuckets = new[] { "25k-50k", "50k-75k", "75k-100k", "100k-150k", "> 150k" };
        var countries = new[] { "US", "US", "US", "CA", "UK", "AU", "DE", "FR" };
        var postalCodes = new Dictionary<string, string[]>
        {
            ["US"] = new[] { "10001", "90210", "60601", "02108", "30301", "33101", "94102", "98101", "75201", "85001", "97201", "80202" },
            ["CA"] = new[] { "M5H 2N2", "V6B 1A1", "T2P 1J9", "K1A 0A9", "H2X 1L4" },
            ["UK"] = new[] { "SW1A 1AA", "E1 6AN", "M1 1AE", "B1 1AA", "LS1 1UR", "EH1 1YZ" },
            ["AU"] = new[] { "2000", "3000", "4000", "5000", "6000" },
            ["DE"] = new[] { "10115", "80331", "20095", "50667" },
            ["FR"] = new[] { "75001", "69001", "13001", "31000" }
        };
        var devices = new[] { "Mobile", "Desktop", "Tablet" };
        var browsers = new[] { "Chrome", "Safari", "Firefox", "Edge" };
        var interestPool = new[]
        {
            "technology,gaming,music,fitness",
            "sports,fitness,automotive,travel",
            "finance,travel,shopping,food",
            "technology,finance,outdoor,fitness",
            "health,wellness,fitness,travel,luxury",
            "family,home,automotive,finance,shopping",
            "shopping,family,education,health,food",
            "automotive,outdoor,family,finance,technology",
            "health,wellness,luxury,travel,culture",
            "finance,travel,automotive,technology,culture",
            "shopping,family,health,travel,home",
            "finance,travel,luxury,health,culture",
            "travel,culture,wellness,shopping,luxury",
            "technology,finance,travel,health,outdoor",
            "travel,culture,health,luxury,family",
            "travel,culture,health,finance,outdoor",
            "technology,health,food,culture,shopping",
            "finance,technology,family,culture,travel",
            "fashion,music,food,shopping,travel",
            "technology,finance,automotive,culture,travel"
        };

        var rng = new Random(12345);

        for (int i = 21; i <= 100; i++)
        {
            var id = $"panelist-{i:D3}";
            var ageRangeIdx = rng.Next(ageRanges.Length);
            var age = rng.Next(ageRangeMins[ageRangeIdx], ageRangeMaxs[ageRangeIdx] + 1);
            var gender = genders[rng.Next(genders.Length)];
            var country = countries[rng.Next(countries.Length)];
            var codes = postalCodes[country];
            var postalCode = codes[rng.Next(codes.Length)];
            var incomeBucket = incomeBuckets[rng.Next(incomeBuckets.Length)];
            var device = devices[rng.Next(devices.Length)];
            var browser = browsers[rng.Next(browsers.Length)];
            var interests = interestPool[rng.Next(interestPool.Length)];
            var daysAgoCreated = rng.Next(10, 300);
            var daysAgoActive = rng.Next(0, 14);
            var consentGdpr = rng.NextDouble() > 0.1;
            var consentCcpa = country == "US" || country == "CA" ? rng.NextDouble() > 0.15 : false;
            var isActive = rng.NextDouble() > 0.05;

            panelists.Add(new
            {
                id = id,
                panelistId = id,
                email = "nupurabhi1@gmail.com",
                firstName = firstNames[(i - 21) % firstNames.Length],
                lastName = lastNames[(i - 21) % lastNames.Length],
                age = age,
                ageRange = ageRanges[ageRangeIdx],
                gender = gender,
                hhIncomeBucket = incomeBucket,
                interests = interests,
                country = country,
                postalCode = postalCode,
                deviceType = device,
                browser = browser,
                consentGdpr = consentGdpr,
                consentCcpa = consentCcpa,
                consentGiven = consentGdpr || consentCcpa,
                consentTimestamp = DateTime.UtcNow.AddDays(-daysAgoCreated),
                lastActive = DateTime.UtcNow.AddDays(-daysAgoActive),
                pointsBalance = 0,
                isActive = isActive,
                createdAt = DateTime.UtcNow.AddDays(-daysAgoCreated),
                updatedAt = DateTime.UtcNow.AddDays(-daysAgoActive)
            });
        }

        return panelists;
    }
}
