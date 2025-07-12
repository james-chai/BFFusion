using System.Text.Json.Serialization;

namespace BFFusion.Server.IdentityModel;

public record TokenResponse
{
    [JsonIgnore]
    public bool IsError { get; init; }
    [JsonIgnore]
    public string? Error { get;init; }
    [JsonPropertyName("access_token")]
    public string? AccessToken { get;init; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get;init; }
    [JsonIgnore]
    public string? Raw { get;init; } // Full JSON response (optional)
}
