using System;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace GitHubProxy.Proxy
{
    public class GitHubProxyUniversalTransformer : ITransformProvider
    {
        public void ValidateCluster(TransformClusterValidationContext context) { }
        public void ValidateRoute(TransformRouteValidationContext context) { }
        public void Apply(TransformBuilderContext context)
        {
            if (context.Route.RouteId.StartsWith("__github_", StringComparison.Ordinal))
            {
                context.AddRequestHeader("Referer", "https://github.com", false);
                context.AddRequestTransform(context =>
                {
                    context.ProxyRequest.Headers.Remove("Origin");
                    return default;
                });
                context.AddResponseTransform(context =>
                {
                    IHeaderDictionary headers = context.HttpContext.Response.Headers;
                    headers.Remove("Accept-Ranges");
                    headers.Remove("Content-Security-Policy");
                    headers.Remove("Strict-Transport-Security");
                    return default;
                });
            }
        }
    }
}
