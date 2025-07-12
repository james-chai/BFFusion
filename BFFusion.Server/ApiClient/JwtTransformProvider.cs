using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace BFFusion.Server.ApiClient;

public class JwtTransformProvider(IAccessTokenProvider tokenProvider, ILogger<JwtTransformProvider> logger)
    : ITransformProvider
{
    private const string TargetRouteId = "downstreamapiroute";

    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.RouteId == TargetRouteId)
        {
            context.AddRequestTransform(async transformContext =>
            {
                try
                {
                    var accessToken = await tokenProvider.GetApiTokenAsync();
                    transformContext.ProxyRequest.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to acquire token for downstream API");
                    throw;
                }
            });
        }
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        // No validation needed for this transform
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
        // Optional: Add validation logic here
    }
}
