namespace BFFusion.Server.IdentityModel;

public class ClientCredentialsTokenRequest
{
    public required string Address { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required IEnumerable<string> Scopes { get; init; }
    public string ScopeString => string.Join(" ", Scopes);
}
