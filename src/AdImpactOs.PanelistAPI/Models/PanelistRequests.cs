using Newtonsoft.Json;

namespace AdImpactOs.PanelistAPI.Models;

/// <summary>
/// Request model for creating a new panelist
/// </summary>
public class CreatePanelistRequest
{
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    [JsonProperty("firstName")]
    public string? FirstName { get; set; }

    [JsonProperty("lastName")]
    public string? LastName { get; set; }

    [JsonProperty("age")]
    public int? Age { get; set; }

    [JsonProperty("ageRange")]
    public string? AgeRange { get; set; }

    [JsonProperty("gender")]
    public string? Gender { get; set; }

    [JsonProperty("hhIncomeBucket")]
    public string? HhIncomeBucket { get; set; }

    [JsonProperty("interests")]
    public string? Interests { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("postalCode")]
    public string? PostalCode { get; set; }

    [JsonProperty("deviceType")]
    public string? DeviceType { get; set; }

    [JsonProperty("browser")]
    public string? Browser { get; set; }

    [JsonProperty("consentGdpr")]
    public bool ConsentGdpr { get; set; } = false;

    [JsonProperty("consentCcpa")]
    public bool ConsentCcpa { get; set; } = false;

    [JsonProperty("consentGiven")]
    public bool ConsentGiven { get; set; } = false;
}

/// <summary>
/// Request model for updating a panelist
/// </summary>
public class UpdatePanelistRequest
{
    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    [JsonProperty("firstName")]
    public string? FirstName { get; set; }

    [JsonProperty("lastName")]
    public string? LastName { get; set; }

    [JsonProperty("age")]
    public int? Age { get; set; }

    [JsonProperty("ageRange")]
    public string? AgeRange { get; set; }

    [JsonProperty("gender")]
    public string? Gender { get; set; }

    [JsonProperty("hhIncomeBucket")]
    public string? HhIncomeBucket { get; set; }

    [JsonProperty("interests")]
    public string? Interests { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("postalCode")]
    public string? PostalCode { get; set; }

    [JsonProperty("deviceType")]
    public string? DeviceType { get; set; }

    [JsonProperty("browser")]
    public string? Browser { get; set; }

    [JsonProperty("consentGdpr")]
    public bool? ConsentGdpr { get; set; }

    [JsonProperty("consentCcpa")]
    public bool? ConsentCcpa { get; set; }

    [JsonProperty("consentGiven")]
    public bool? ConsentGiven { get; set; }

    [JsonProperty("isActive")]
    public bool? IsActive { get; set; }

    [JsonProperty("pointsBalance")]
    public int? PointsBalance { get; set; }
}
