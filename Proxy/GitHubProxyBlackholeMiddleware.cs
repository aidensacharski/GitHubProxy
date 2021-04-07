using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GitHubProxy.Proxy
{
    public class GitHubProxyBlackholeMiddleware
    {
        private readonly string? _blackholeAuthority;
        private readonly RequestDelegate _next;

        public GitHubProxyBlackholeMiddleware(IGitHubProxyConfiguration configuration, RequestDelegate next)
        {
            _blackholeAuthority = configuration.IsConfigured ? configuration.BlackholeDomainAuthority : null;
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (_blackholeAuthority is not null && context.Request.Host.Equals(new HostString(_blackholeAuthority)))
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            }
            return _next(context);
        }
    }
}
