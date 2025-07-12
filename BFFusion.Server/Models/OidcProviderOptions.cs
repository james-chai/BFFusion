namespace BFFusion.Server.Models;

public record OidcProviderOptions
{
    public string Authority { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CallbackPath { get; init; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; init; } = "/signout-oidc";
    public string[] Scopes { get; init; } = ["openid", "profile"];
    public string ResponseType { get; init; } = "code";
    public bool GetClaimsFromUserInfoEndpoint { get; init; } = true;
    public bool SaveTokens { get; init; } = true;
    public string[] DownstreamApiScopes { get; init; } = [];
}
