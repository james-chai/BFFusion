using System.Security.Claims;
using BFFusion.Server.Infrastructure;
using BFFusion.Server.Models;

namespace BFFusion.Server.Endpoints;

public class User : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
           .MapGet("/", GetCurrentUser)
           .AllowAnonymous()
           .WithTags(nameof(User))
           .WithOpenApi()
           .Produces<UserInfo>(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status401Unauthorized)
           .WithDescription("Gets information about the current user");
    }

    public static IResult GetCurrentUser(HttpContext context)
    {
        var userInfo = CreateUserInfo(context.User);
        return TypedResults.Ok(userInfo);
    }

    private static UserInfo CreateUserInfo(ClaimsPrincipal claimsPrincipal)
    {
        if (!claimsPrincipal?.Identity?.IsAuthenticated ?? true)
        {
            return UserInfo.Anonymous;
        }

        var userInfo = new UserInfo
        {
            IsAuthenticated = true
        };

        if (claimsPrincipal?.Identity is ClaimsIdentity claimsIdentity)
        {
            userInfo.NameClaimType = claimsIdentity.NameClaimType;
            userInfo.RoleClaimType = claimsIdentity.RoleClaimType;
        }
        else
        {
            userInfo.NameClaimType = ClaimTypes.Name;
            userInfo.RoleClaimType = ClaimTypes.Role;
        }

        if (claimsPrincipal?.Claims?.Any() ?? false)
        {
            var claims = claimsPrincipal.FindAll(userInfo.NameClaimType)
                                      .Select(u => new ClaimValue(userInfo.NameClaimType, u.Value))
                                      .ToList();

            userInfo.Claims = claims;
        }

        return userInfo;
    }
}
