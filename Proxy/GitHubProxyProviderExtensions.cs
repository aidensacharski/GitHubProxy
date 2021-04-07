using GitHubProxy.Proxy;
using Yarp.ReverseProxy.Service;
using Yarp.ReverseProxy.Service.Proxy.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GitHubProxyProviderExtensions
    {
        public static IReverseProxyBuilder AddGitHubProxy(this IReverseProxyBuilder builder)
        {
            builder.Services.AddSingleton<IProxyConfigProvider, GitHubProxyProvider>();
            builder.Services.AddSingleton<IProxyHttpClientFactory, GitHubProxyHttpClientFactory>();
            builder.AddTransforms<GitHubProxyMainSiteTransformer>();
            builder.AddTransforms<GitHubProxyUniversalTransformer>();

            builder.Services.AddSingleton<IGitHubProxyConfiguration, GitHubProxyConfiguration>();

            return builder;
        }
    }
}
