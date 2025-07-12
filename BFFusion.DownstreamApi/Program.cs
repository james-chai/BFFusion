using BFFusion.DownstreamApi.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration Setup =====
var oidcConfig = builder.Configuration.GetSection("OidcConfig");
var swaggerConfig = builder.Configuration.GetSection("Swagger");
var baseUrl = builder.Configuration.GetSection("BaseUrl") ?? throw new ArgumentException("Base Url not defined");

// ===== Service Configuration =====
ConfigureCors(builder.Services, baseUrl);
ConfigureAuthentication(builder.Services, oidcConfig);
ConfigureSwagger(builder.Services, swaggerConfig);

var app = builder.Build();

// ===== Middleware Pipeline =====
ConfigureMiddleware(app);

// ===== Endpoints =====
app.MapEndpoints();

// ===== Development-Specific Setup =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
}

app.Run();

// ===== Configuration Methods =====
static void ConfigureCors(IServiceCollection services, IConfigurationSection baseUrl)
{
    services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
        {
            policy.WithOrigins(baseUrl.Value!)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

static void ConfigureAuthentication(IServiceCollection services, IConfigurationSection oidcConfig)
{
    services.AddAuthentication()
        .AddJwtBearer("Bearer", options =>
        {
            // {TENANT ID} is the directory (tenant) ID.
            //
            // Authority format {AUTHORITY} matches the issurer (`iss`) of the JWT returned by the identity provider.
            //
            // Authority format {AUTHORITY} for ME-ID tenant type: https://sts.windows.net/{TENANT ID}/
            // Authority format {AUTHORITY} for B2C tenant type: https://login.microsoftonline.com/{TENANT ID}/v2.0/
            //
            options.Authority = oidcConfig["Authority"];
            //
            // The following should match just the path of the Application ID URI configured when adding the "Weather.Get" scope
            // under "Expose an API" in the Azure or Entra portal. {CLIENT ID} is the application (client) ID of this 
            // app's registration in the Azure portal.
            // 
            // Audience format {AUDIENCE} for ME-ID tenant type: api://{CLIENT ID}
            // Audience format {AUDIENCE} for B2C tenant type: https://{DIRECTORY NAME}.onmicrosoft.com/{CLIENT ID}
            //
            options.Audience = oidcConfig["Audience"];
        });

    services.AddAuthorization();
}

static void ConfigureSwagger(IServiceCollection services, IConfigurationSection swaggerConfig)
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = swaggerConfig["Title"],
            Version = swaggerConfig["Version"]
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            Description = "Paste a valid access token (obtained separately)",
            In = ParameterLocation.Header
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        c.DocInclusionPredicate((name, api) => true);
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseHttpsRedirection();
    app.UseCors("CorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
}
