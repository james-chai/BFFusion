using System.Text.Json.Serialization;

namespace BFFusion.Server.IdentityModel;

public record DiscoveryDocumentResponse
{
    [JsonIgnore]
    public bool IsError { get; init; }
    [JsonIgnore]
    public string? Error { get; init; }
    [JsonPropertyName("token_endpoint")]
    public string? TokenEndpoint { get; init; }
    [JsonIgnore]
    public string? Raw { get; init; } // Full JSON response (optional)
}
