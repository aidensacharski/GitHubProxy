using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace GitHubProxy.Proxy
{
    public class GitHubProxyHttpClientFactory : ForwarderHttpClientFactory
    {
        private IGitHubProxyConfiguration _configuration;

        public GitHubProxyHttpClientFactory(IGitHubProxyConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void ConfigureHandler(ForwarderHttpClientContext context, SocketsHttpHandler handler)
        {
            base.ConfigureHandler(context, handler);

            handler.Proxy = (!_configuration.UseProxy) || string.IsNullOrEmpty(_configuration.Proxy) ? null : new WebProxy(_configuration.Proxy);
            handler.UseProxy = _configuration.UseProxy;
        }
    }
}
