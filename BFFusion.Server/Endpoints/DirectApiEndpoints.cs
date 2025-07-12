using BFFusion.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BFFusion.Server.Endpoints;

public class DirectApi : EndpointGroupBase
{
    private static readonly string[] SampleData =
    {
        "some data",
        "more data",
        "loads of data"
    };

    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
           .RequireAuthorization(policy => policy
               .RequireAuthenticatedUser()
               .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme))
           .MapGet("/", GetData)
           .WithTags(nameof(DirectApi))
           .WithOpenApi()
           .Produces<string[]>(StatusCodes.Status200OK);
    }

    public Ok<string[]> GetData()
    {
        return TypedResults.Ok(SampleData);
    }
}
