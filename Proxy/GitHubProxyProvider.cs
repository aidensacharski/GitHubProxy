using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace GitHubProxy.Proxy
{
    public class GitHubProxyProvider : IProxyConfigProvider
    {
        private readonly InMemoryConfig _config;

        public GitHubProxyProvider(IGitHubProxyConfiguration configuration)
        {
            if (!configuration.IsConfigured)
            {
                _config = new InMemoryConfig(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());
                return;
            }

            _config = new InMemoryConfig(
                new RouteConfig[]
                {
                    new ()
                    {
                        RouteId = "__github_mainRoute",
                        ClusterId = "__github_homeCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.HomeDomainAuthority }
                        }
                    },
                    new ()
                    {
                        RouteId = "__github_assetsRoute",
                        ClusterId = "__github_assetsCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.AssetsDomainAuthority }
                        }
                    },
                    new ()
                    {
                        RouteId = "__github_avatarsRoute",
                        ClusterId = "__github_avatarsCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.AvatarsDomainAuthority }
                        }
                    },
                    new ()
                    {
                        RouteId = "__github_rawRoute",
                        ClusterId = "__github_rawCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.RawDomainAuthority }
                        }
                    },
                    new ()
                    {
                        RouteId = "__github_camoRoute",
                        ClusterId = "__github_camoCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.CamoDomainAuthority }
                        }
                    },
                    new ()
                    {
                        RouteId = "__github_codeloadRoute",
                        ClusterId = "__github_codeloadCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.CodeloadDomainAuthority }
                        }
                    },
                    new ()
                    {
                        RouteId = "__github_releasesRoute",
                        ClusterId = "__github_releasesCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.ReleasesDomainAuthority }
                        }
                    },
                    new()
                    {
                        RouteId = "__github_userImagesRoute",
                        ClusterId = "__github_userImagesCluster",
                        Match = new RouteMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.UserImagesDomainAuthority }
                        }
                    }
                },
                new ClusterConfig[]
                {
                    new ()
                    {
                        ClusterId = "__github_homeCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://github.com" } }
                        }
                    },
                    new ()
                    {
                        ClusterId = "__github_assetsCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://github.githubassets.com" } }
                        }
                    },
                    new ()
                    {
                        ClusterId = "__github_avatarsCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://avatars.githubusercontent.com" } }
                        }
                    },
                    new ()
                    {
                        ClusterId = "__github_rawCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://raw.githubusercontent.com" } }
                        }
                    },
                    new ()
                    {
                        ClusterId = "__github_camoCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://camo.githubusercontent.com" } }
                        }
                    },
                    new ()
                    {
                        ClusterId = "__github_codeloadCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://codeload.github.com" } }
                        }
                    },
                    new ()
                    {
                        ClusterId = "__github_releasesCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://github-releases.githubusercontent.com" } }
                        }
                    },
                    new()
                    {
                        ClusterId = "__github_userImagesCluster",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://user-images.githubusercontent.com" } }
                        }
                    },
                });
        }

        public IProxyConfig GetConfig() => _config;
    }

    internal class InMemoryConfig : IProxyConfig
    {
        public InMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(CancellationToken.None);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }

        public IReadOnlyList<ClusterConfig> Clusters { get; }

        public IChangeToken ChangeToken { get; }
    }
}
