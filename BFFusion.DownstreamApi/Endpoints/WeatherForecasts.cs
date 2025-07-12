using BFFusion.DownstreamApi.Infrastructure;
using BFFusion.DownstreamApi.Models;

namespace BFFusion.DownstreamApi.Endpoints;

public class WeatherForecasts : EndpointGroupBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
           .RequireAuthorization()
           .MapGet("/", GetWeatherForecasts)
           .WithTags(nameof(WeatherForecasts))
           .WithOpenApi()
           .Produces<WeatherForecast[]>(StatusCodes.Status200OK);
    }

    public IResult GetWeatherForecasts()
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ))
            .ToArray();

        return TypedResults.Ok(forecast);
    }
}
