using GitHubProxy.Proxy;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GitHubProxyProviderExtensions
    {
        public static IReverseProxyBuilder AddGitHubProxy(this IReverseProxyBuilder builder)
        {
            builder.Services.AddSingleton<IProxyConfigProvider, GitHubProxyProvider>();
            builder.Services.AddSingleton<IForwarderHttpClientFactory, GitHubProxyHttpClientFactory>();
            builder.AddTransforms<GitHubProxyMainSiteTransformer>();
            builder.AddTransforms<GitHubProxyUniversalTransformer>();

            builder.Services.AddSingleton<IGitHubProxyConfiguration, GitHubProxyConfiguration>();

            return builder;
        }
    }
}
