using BFFusion.Server.ApiClient;
using BFFusion.Server.Endpoints;
using BFFusion.Server.Infrastructure;
using BFFusion.Server.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Yarp.ReverseProxy.Transforms.Builder;

var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder);

var app = builder.Build();
ConfigureMiddleware(app);
ConfigureEndpoints(app);

app.Run();

// ========== Service Configuration ==========
static void ConfigureServices(WebApplicationBuilder builder)
{
    var services = builder.Services;
    var configuration = builder.Configuration;
    var env = builder.Environment;

    // Development-specific settings
    if (env.IsDevelopment())
    {
        IdentityModelEventSource.ShowPII = true;
        IdentityModelEventSource.LogCompleteSecurityArtifact = true;
    }

    ConfigureCors(services, configuration);
    ConfigureAntiforgery(services);
    ConfigureSwagger(services);
    ConfigureAuthentication(services, configuration);
    ConfigureApplicationServices(services);
    ConfigureReverseProxy(services, configuration);
}

static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
{
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    services.AddCors(options => options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    }));
}

static void ConfigureAntiforgery(IServiceCollection services)
{
    services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "__Host-X-XSRF-TOKEN";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
}

static void ConfigureSwagger(IServiceCollection services)
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "BFFusion Server API",
            Version = "v1",
            Description = "Backend for Frontend API Gateway"
        });

        c.DocInclusionPredicate((name, api) => true);
    });
}

static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<OidcProviderOptions>(configuration.GetSection("OidcProvider"));

    services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "oidc_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(options =>
    {
        var oidcConfig = configuration.GetSection("OidcProvider").Get<OidcProviderOptions>()!;

        options.Authority = oidcConfig.Authority;
        options.ClientId = oidcConfig.ClientId;
        options.ClientSecret = oidcConfig.ClientSecret;
        options.CallbackPath = oidcConfig.CallbackPath;
        options.SignedOutCallbackPath = oidcConfig.SignedOutCallbackPath;
        options.ResponseType = oidcConfig.ResponseType;
        options.GetClaimsFromUserInfoEndpoint = oidcConfig.GetClaimsFromUserInfoEndpoint;
        options.SaveTokens = oidcConfig.SaveTokens;

        foreach (var scope in oidcConfig.Scopes)
            options.Scope.Add(scope);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            ValidateIssuer = true,
            ValidIssuer = new Uri(oidcConfig.Authority).Authority
        };
    });
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddHttpClient();
    services.AddDistributedMemoryCache();
    services.AddSingleton<IAccessTokenProvider, ApiTokenCacheClient>();
    services.AddSingleton<ITransformProvider, JwtTransformProvider>();
    services.AddScoped<Account>();
    services.AddScoped<User>();
}

static void ConfigureReverseProxy(IServiceCollection services, IConfiguration configuration)
{
    services.AddReverseProxy()
        .LoadFromConfig(configuration.GetSection("ReverseProxy"))
        .AddTransforms<JwtTransformProvider>();
}

// ========== Middleware Configuration ==========
static void ConfigureMiddleware(WebApplication app)
{
    app.UseCors("DevCorsPolicy");

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1"));
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
}

// ========== Endpoint Configuration ==========
static void ConfigureEndpoints(WebApplication app)
{
    app.MapEndpoints();
    app.MapReverseProxy().RequireCors("DevCorsPolicy");

    if (!app.Environment.IsDevelopment())
    {
        app.MapFallbackToFile("index.html");
    }
}
