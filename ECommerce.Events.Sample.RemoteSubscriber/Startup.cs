using System.Diagnostics;
using ECommerce.Events.Clients.SubscriberClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Events.Sample.RemoteSubscriber
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSubscriber(Configuration, _loggerFactory, _diagnosticSource);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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