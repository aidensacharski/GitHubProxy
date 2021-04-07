using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Yarp.ReverseProxy.Abstractions;
using Yarp.ReverseProxy.Service.Proxy.Infrastructure;

namespace GitHubProxy.Proxy
{
    public class GitHubProxyHttpClientFactory : IProxyHttpClientFactory
    {
        private IGitHubProxyConfiguration _configuration;

        public GitHubProxyHttpClientFactory(IGitHubProxyConfiguration configuration)
        {
            _configuration = configuration;
        }

        public HttpMessageInvoker CreateClient(ProxyHttpClientContext context)
        {
            if (context.OldClient != null && context.NewOptions == context.OldOptions)
            {
                return context.OldClient;
            }

            ProxyHttpClientOptions newClientOptions = context.NewOptions;

            var handler = new SocketsHttpHandler
            {
                Proxy = (!_configuration.UseProxy) || string.IsNullOrEmpty(_configuration.Proxy) ? null : new WebProxy(_configuration.Proxy),
                UseProxy = _configuration.UseProxy,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false
            };

            if (newClientOptions.SslProtocols.HasValue)
            {
                handler.SslOptions.EnabledSslProtocols = newClientOptions.SslProtocols.Value;
            }
            if (newClientOptions.ClientCertificate != null)
            {
                handler.SslOptions.ClientCertificates = new X509CertificateCollection
                {
                    newClientOptions.ClientCertificate
                };
            }
            if (newClientOptions.MaxConnectionsPerServer != null)
            {
                handler.MaxConnectionsPerServer = newClientOptions.MaxConnectionsPerServer.Value;
            }
            if (newClientOptions.DangerousAcceptAnyServerCertificate.GetValueOrDefault())
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
            }
            if (newClientOptions.RequestHeaderEncoding != null)
            {
                handler.RequestHeaderEncodingSelector = (_, _) => newClientOptions.RequestHeaderEncoding;
            }

            return new HttpMessageInvoker(handler, disposeHandler: true);
        }
    }
}
