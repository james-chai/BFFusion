using System.Text.Json;
using BFFusion.Server.IdentityModel;
using BFFusion.Server.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace BFFusion.Server.ApiClient;

public class ApiTokenCacheClient(
    IHttpClientFactory httpClientFactory,
    ILogger<ApiTokenCacheClient> logger,
    IDistributedCache cache,
    IOptions<OidcProviderOptions> oidcProviderOptions) : IAccessTokenProvider
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly ILogger<ApiTokenCacheClient> _logger = logger;
    private readonly IDistributedCache _cache = cache;
    private readonly OidcProviderOptions _oidcConfig = oidcProviderOptions.Value;
    private const int cacheExpirationInMinutes = 50;
    private static readonly object _lock = new();
    private const string CacheKey = "downstream_api_access_token";

    private record AccessTokenItem(string AccessToken, DateTime ExpiresIn);

    public async Task<string> GetApiTokenAsync()
    {
        var cachedToken = GetFromCache();

        if (cachedToken is not null && cachedToken.ExpiresIn > DateTime.UtcNow)
        {
            return cachedToken.AccessToken;
        }

        // (1) Fetch OIDC metadata manually
        var disco = await GetDiscoveryDocumentAsync(_oidcConfig.Authority);
        if (disco.IsError)
        {
            _logger.LogError("Discovery failed: {error}", disco.Error);
            throw new Exception($"Discovery failed: {disco.Error}");
        }

        // (2) Request token manually
        var tokenResponse = await RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint!,
            ClientId = _oidcConfig.ClientId,
            ClientSecret = _oidcConfig.ClientSecret,
            Scopes = _oidcConfig.DownstreamApiScopes
        });

        if (tokenResponse.IsError || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            _logger.LogError("Token request failed: {error}", tokenResponse.Error);
            throw new Exception($"Token request failed: {tokenResponse.Error}");
        }

        var newToken = new AccessTokenItem(
            AccessToken: tokenResponse.AccessToken,
            ExpiresIn: DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        );

        AddToCache(CacheKey, newToken);
        return newToken.AccessToken;
    }

    public async Task<TokenResponse> RequestClientCredentialsTokenAsync(ClientCredentialsTokenRequest request)
    {
        var form = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", request.ClientId },
            { "client_secret", request.ClientSecret },
            { "scope", request.ScopeString }
        };

        var response = await _httpClient.PostAsync(request.Address, new FormUrlEncodedContent(form));

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            return new TokenResponse
            {
                IsError = true,
                Error = $"Token request failed: {response.StatusCode}",
                Raw = errorJson
            };
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenResponse>(json) ??
               throw new InvalidOperationException("Failed to parse token response");
    }

    public async Task<DiscoveryDocumentResponse> GetDiscoveryDocumentAsync(string authority)
    {
        var discoveryUrl = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
        var response = await _httpClient.GetAsync(discoveryUrl);

        if (!response.IsSuccessStatusCode)
        {
            return new DiscoveryDocumentResponse
            {
                IsError = true,
                Error = $"Discovery request failed: {response.StatusCode}"
            };
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DiscoveryDocumentResponse>(json) ??
               throw new InvalidOperationException("Failed to parse discovery document");
    }

    private void AddToCache(string key, AccessTokenItem token)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpirationInMinutes)
        };

        lock (_lock)
        {
            var serialized = JsonSerializer.Serialize(token);
            _cache.SetString(key, serialized, options);
        }
    }

    private AccessTokenItem? GetFromCache()
    {
        var json = _cache.GetString(CacheKey);

        return json is null ? null : JsonSerializer.Deserialize<AccessTokenItem>(json);
    }
}
