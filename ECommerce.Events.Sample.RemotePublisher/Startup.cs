using System.Diagnostics;
using ECommerce.Events.Clients.PublisherClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Events.Sample.RemotePublisher
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly DiagnosticSource _diagnosticSource;

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource)
        {
            _loggerFactory = loggerFactory;
            _diagnosticSource = diagnosticSource;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddPublisher(Configuration, _loggerFactory, _diagnosticSource);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
