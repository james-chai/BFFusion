namespace BFFusion.Server.ApiClient;

public interface IAccessTokenProvider
{
    Task<string> GetApiTokenAsync();
}
