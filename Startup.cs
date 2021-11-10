using GitHubProxy.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitHubProxy
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<GitHubProxyOptions>(Configuration.GetSection("Proxy"));

            services.AddReverseProxy().AddGitHubProxy();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<GitHubProxyBlackholeMiddleware>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/robots.txt", () => "User-agent: *\nDisallow: /");
                endpoints.MapReverseProxy();
            });

            app.ApplicationServices.GetRequiredService<ILogger<Startup>>().LogInformation("GitHubProxy Version: {0}", ThisAssembly.AssemblyInformationalVersion);
        }
    }
}
