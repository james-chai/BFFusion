using BFFusion.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace BFFusion.Server.Endpoints;

public class Account : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .WithTags("Account")
            .WithOpenApi();

        group.MapGet("/login", Login)
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status400BadRequest)
            .WithDescription("Initiates OpenID Connect authentication flow");

        group.MapPost("/logout", [IgnoreAntiforgeryToken] (
            [FromQuery] string redirectUrl,
            HttpContext context) =>
        {
            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = redirectUrl ?? "/" },
                new[] {
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                });
        })
        .RequireAuthorization()
        .Produces(StatusCodes.Status302Found)
        .WithDescription("Terminates the current user session");
    }

    public static IResult Login(
        [FromQuery] string? returnUrl,
        [FromQuery] string? claimsChallenge,
        HttpContext context)
    {
        var properties = GetAuthProperties(context, returnUrl);

        if (claimsChallenge != null)
        {
            string jsonString = claimsChallenge.Replace("\\", "")
                .Trim(new char[1] { '"' });
            properties.Items["claims"] = jsonString;
        }

        return Results.Challenge(
            properties,
            new[] { OpenIdConnectDefaults.AuthenticationScheme });
    }

    public static IResult Logout(
        HttpContext context,
        [FromQuery] string redirectUrl = "/")
    {
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = redirectUrl },
            new[] {
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            });
    }

    private static AuthenticationProperties GetAuthProperties(HttpContext httpContext, string? returnUrl, bool isLogout = false)
    {
        var pathBase = httpContext.Request.PathBase.HasValue
            ? httpContext.Request.PathBase.Value
            : "/";

        // Prevent open redirects.
        if (isLogout || string.IsNullOrEmpty(returnUrl))   // Redirect to the Home page after logout
        {
            returnUrl = pathBase;
        }
        else if (Uri.IsWellFormedUriString(returnUrl, UriKind.Absolute))
        {
            // Allow known absolute URLs
            var allowedHosts = new[] { "https://localhost:3000" };

            if (!allowedHosts.Any(allowed => returnUrl.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
            {
                // Invalid origin → fallback to home
                returnUrl = pathBase;
            }
        }
        else if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            returnUrl = new Uri(returnUrl, UriKind.Absolute).PathAndQuery;
        }
        else if (returnUrl[0] != '/')
        {
            returnUrl = $"{pathBase}{returnUrl}";
        }

        return new AuthenticationProperties { RedirectUri = returnUrl };
    }
}
