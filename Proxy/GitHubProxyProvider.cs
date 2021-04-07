using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Abstractions;
using Yarp.ReverseProxy.Service;

namespace GitHubProxy.Proxy
{
    public class GitHubProxyProvider : IProxyConfigProvider
    {
        private readonly InMemoryConfig _config;

        public GitHubProxyProvider(IGitHubProxyConfiguration configuration)
        {
            if (!configuration.IsConfigured)
            {
                _config = new InMemoryConfig(Array.Empty<ProxyRoute>(), Array.Empty<Cluster>());
                return;
            }

            _config = new InMemoryConfig(
                new[]
                {
                    new ProxyRoute()
                    {
                        RouteId = "__github_mainRoute",
                        ClusterId = "__github_homeCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.HomeDomainAuthority }
                        }
                    },
                    new ProxyRoute()
                    {
                        RouteId = "__github_assetsRoute",
                        ClusterId = "__github_assetsCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.AssetsDomainAuthority }
                        }
                    },
                    new ProxyRoute()
                    {
                        RouteId = "__github_avatarsRoute",
                        ClusterId = "__github_avatarsCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.AvatarsDomainAuthority }
                        }
                    },
                    new ProxyRoute()
                    {
                        RouteId = "__github_rawRoute",
                        ClusterId = "__github_rawCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.RawDomainAuthority }
                        }
                    },
                    new ProxyRoute()
                    {
                        RouteId = "__github_camoRoute",
                        ClusterId = "__github_camoCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.CamoDomainAuthority }
                        }
                    },
                    new ProxyRoute()
                    {
                        RouteId = "__github_codeloadRoute",
                        ClusterId = "__github_codeloadCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.CodeloadDomainAuthority }
                        }
                    },
                    new ProxyRoute()
                    {
                        RouteId = "__github_releasesRoute",
                        ClusterId = "__github_releasesCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.ReleasesDomainAuthority }
                        }
                    },
                    new ProxyRoute()
                    {
                        RouteId = "__github_userImagesRoute",
                        ClusterId = "__github_userImagesCluster",
                        Match = new ProxyMatch
                        {
                            Path = "{**catch-all}",
                            Hosts = new [] { configuration.UserImagesDomainAuthority }
                        }
                    }
                },
                new[]
                {
                    new Cluster()
                    {
                        Id = "__github_homeCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://github.com" } }
                        }
                    },
                    new Cluster()
                    {
                        Id = "__github_assetsCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://github.githubassets.com" } }
                        }
                    },
                    new Cluster()
                    {
                        Id = "__github_avatarsCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://avatars.githubusercontent.com" } }
                        }
                    },
                    new Cluster()
                    {
                        Id = "__github_rawCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://raw.githubusercontent.com" } }
                        }
                    },
                    new Cluster()
                    {
                        Id = "__github_camoCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://camo.githubusercontent.com" } }
                        }
                    },
                    new Cluster()
                    {
                        Id = "__github_codeloadCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://codeload.github.com" } }
                        }
                    },
                    new Cluster()
                    {
                        Id = "__github_releasesCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://github-releases.githubusercontent.com" } }
                        }
                    },
                    new Cluster()
                    {
                        Id = "__github_userImagesCluster",
                        Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new Destination() { Address = "https://user-images.githubusercontent.com" } }
                        }
                    },
                });
        }

        public IProxyConfig GetConfig() => _config;
    }

    internal class InMemoryConfig : IProxyConfig
    {
        public InMemoryConfig(IReadOnlyList<ProxyRoute> routes, IReadOnlyList<Cluster> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(CancellationToken.None);
        }

        public IReadOnlyList<ProxyRoute> Routes { get; }

        public IReadOnlyList<Cluster> Clusters { get; }

        public IChangeToken ChangeToken { get; }
    }
}
