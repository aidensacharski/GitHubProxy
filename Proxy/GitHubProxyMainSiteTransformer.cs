using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHubProxy.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Abstractions.Config;
using Yarp.ReverseProxy.Service.RuntimeModel.Transforms;

namespace GitHubProxy.Proxy
{
    public class GitHubProxyMainSiteTransformer : ITransformProvider
    {
        private readonly string _homeDomain;
        private readonly string _rawDomain;
        private readonly string _codeloadDomain;
        private readonly string _releasesDomain;

        private readonly Utf8StringReplaceDirective[] _directives;
        private readonly ILogger _logger;

        public GitHubProxyMainSiteTransformer(IGitHubProxyConfiguration configuration, ILogger<GitHubProxyMainSiteTransformer> logger)
        {
            _homeDomain = configuration.HomeDomain;
            _rawDomain = configuration.RawDomain;
            _codeloadDomain = configuration.CodeloadDomain;
            _releasesDomain = configuration.ReleasesDomain;

            _directives = new[]
            {
                new Utf8StringReplaceDirective("https://github.com", configuration.HomeDomain),
                new Utf8StringReplaceDirective("https://api.github.com", configuration.BlackholeDomain),
                new Utf8StringReplaceDirective("https://github.githubassets.com", configuration.AssetsDomain),
                new Utf8StringReplaceDirective("https://avatars.githubusercontent.com", configuration.AvatarsDomain),
                new Utf8StringReplaceDirective("https://camo.githubusercontent.com", configuration.CamoDomain),
                new Utf8StringReplaceDirective("https://user-images.githubusercontent.com", configuration.UserImagesDomain),
                new Utf8StringReplaceDirective("collector.githubapp.com", configuration.BlackholeDomainAuthority),
            };
            _logger = logger;
        }

        public void ValidateCluster(TransformClusterValidationContext context) { }
        public void ValidateRoute(TransformRouteValidationContext context) { }
        public void Apply(TransformBuilderContext context)
        {
            if ("__github_mainRoute".Equals(context.Route.RouteId, StringComparison.Ordinal))
            {
                context.AddRequestHeader("Referer", "https://github.com", false);
                context.AddRequestTransform(context =>
                {
                    context.ProxyRequest.Headers.Remove("Accept-Encoding");
                    context.ProxyRequest.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");
                    return default;
                });
                context.AddResponseTransform(TransformAsync);
            }
        }

        private ValueTask TransformAsync(ResponseTransformContext context)
        {
            IHeaderDictionary headers = context.HttpContext.Response.Headers;

            if (headers.TryGetValue("Set-Cookie", out StringValues cookies))
            {
                headers.Remove("Set-Cookie");
                foreach (string item in cookies)
                {
                    headers.Append("Set-Cookie", item.Replace(" Domain=github.com;", "", StringComparison.OrdinalIgnoreCase).Replace(" domain=.github.com;", ""));
                    //.Replace(" SameSite=Strict;", " SameSite=Lax;").Replace(" secure;", "");
                }
            }
            StringValues value;
            if (headers.TryGetValue("Location", out value) && value.Count == 1)
            {
                if (value[0].StartsWith("https://github.com"))
                {
                    headers.Remove("Location");
                    headers.Add("Location", string.Concat(_homeDomain, value[0].AsSpan(18)));
                }
                else if (value[0].StartsWith("https://raw.githubusercontent.com"))
                {
                    headers.Remove("Location");
                    headers.Add("Location", string.Concat(_rawDomain, value[0].AsSpan(33)));
                }
                else if (value[0].StartsWith("https://codeload.github.com"))
                {
                    headers.Remove("Location");
                    headers.Add("Location", string.Concat(_codeloadDomain, value[0].AsSpan(27)));
                }
                else if (value[0].StartsWith("https://github-releases.githubusercontent.com"))
                {
                    headers.Remove("Location");
                    headers.Add("Location", string.Concat(_releasesDomain, value[0].AsSpan(45)));
                }
            }
            if (headers.TryGetValue("x-pjax-url", out value) && value.Count == 1)
            {
                if (value[0].StartsWith("https://github.com"))
                {
                    headers.Remove("x-pjax-url");
                    headers.Add("x-pjax-url", string.Concat(_homeDomain, value[0].AsSpan(18)));
                }
            }

            if ("text/html".Equals(context.ProxyResponse.Content.Headers.ContentType?.MediaType))
            {
                context.HttpContext.Response.ContentLength = null;
                return new ValueTask(ReadAndReplaceAsync(context.ProxyResponse, context.ProxyResponse.Content.Headers.ContentType?.CharSet, context.HttpContext.RequestAborted));
            }
            return default;
        }


        private async Task ReadAndReplaceAsync(HttpResponseMessage response, string? charset, CancellationToken cancellationToken)
        {
            HttpContent content = response.Content;
            Utf8HtmlAttributeReplaceContent replacedContent;
            if ("utf-8".Equals(charset, StringComparison.OrdinalIgnoreCase))
            {
                Stream stream = await content.ReadAsStreamAsync(cancellationToken);
                replacedContent = new Utf8HtmlAttributeReplaceContent(stream, _directives, "text/html", cancellationToken);
            }
            else
            {
                string html = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                content.Dispose();
                replacedContent = new Utf8HtmlAttributeReplaceContent(html, _directives, "text/html", cancellationToken);
            }

            replacedContent.SetLogger(_logger);
            response.Content = replacedContent;
        }
    }
}
