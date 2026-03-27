using Newtonsoft.Json;

namespace AdImpactOs.PanelistAPI.Models;

/// <summary>
/// Panelist profile stored in Cosmos DB.
/// Contains demographic information and consent flags for ad tracking.
/// </summary>
public class Panelist
{
    /// <summary>
    /// Unique identifier (pseudonymized ID) - serves as partition key
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Pseudonymized panelist ID (same as Id for clarity)
    /// </summary>
    [JsonProperty("panelistId")]
    public string PanelistId { get; set; } = string.Empty;

    /// <summary>
    /// Hashed email address (SHA256)
    /// </summary>
    [JsonProperty("hashedEmail")]
    public string? HashedEmail { get; set; }

    /// <summary>
    /// Hashed phone number (SHA256)
    /// </summary>
    [JsonProperty("hashedPhone")]
    public string? HashedPhone { get; set; }

    /// <summary>
    /// Email address (plain text - for internal use only, will be removed in production)
    /// </summary>
    [JsonProperty("email")]
    public string? Email { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    [JsonProperty("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    [JsonProperty("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// Age range bucket (e.g., "18-24", "25-34", "35-44", "45-54", "55-64", "65+")
    /// </summary>
    [JsonProperty("ageRange")]
    public string? AgeRange { get; set; }

    /// <summary>
    /// Exact age (for internal analytics only)
    /// </summary>
    [JsonProperty("age")]
    public int? Age { get; set; }

    /// <summary>
    /// Gender (M/F/Other)
    /// </summary>
    [JsonProperty("gender")]
    public string? Gender { get; set; }

    /// <summary>
    /// Household income bucket (e.g., "<25K", "25K-50K", "50K-75K", "75K-100K", "100K-150K", "150K+")
    /// </summary>
    [JsonProperty("hhIncomeBucket")]
    public string? HhIncomeBucket { get; set; }

    /// <summary>
    /// Interests/categories (comma-separated or JSON array)
    /// </summary>
    [JsonProperty("interests")]
    public string? Interests { get; set; }

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    [JsonProperty("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [JsonProperty("postalCode")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Device type (Desktop/Mobile/Tablet)
    /// </summary>
    [JsonProperty("deviceType")]
    public string? DeviceType { get; set; }

    /// <summary>
    /// Browser type (Chrome/Firefox/Safari/Edge)
    /// </summary>
    [JsonProperty("browser")]
    public string? Browser { get; set; }

    /// <summary>
    /// GDPR consent flag
    /// </summary>
    [JsonProperty("consentGdpr")]
    public bool ConsentGdpr { get; set; } = false;

    /// <summary>
    /// CCPA consent flag (California Consumer Privacy Act)
    /// </summary>
    [JsonProperty("consentCcpa")]
    public bool ConsentCcpa { get; set; } = false;

    /// <summary>
    /// Generic consent flag for ad tracking (legacy)
    /// </summary>
    [JsonProperty("consentGiven")]
    public bool ConsentGiven { get; set; } = false;

    /// <summary>
    /// Timestamp when consent was given
    /// </summary>
    [JsonProperty("consentTimestamp")]
    public DateTime? ConsentTimestamp { get; set; }

    /// <summary>
    /// Last active timestamp - tracks when panelist last interacted with the system
    /// </summary>
    [JsonProperty("lastActive")]
    public DateTime? LastActive { get; set; }

    /// <summary>
    /// Points balance for rewards program
    /// </summary>
    [JsonProperty("pointsBalance")]
    public int PointsBalance { get; set; } = 0;

    /// <summary>
    /// Active status of panelist
    /// </summary>
    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created timestamp
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cosmos DB partition key path
    /// </summary>
    [JsonProperty("_partitionKey")]
    public string PartitionKey => Id;
}
