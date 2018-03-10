using System.Diagnostics;
using System.Net.Http;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Data.Repositories;
using ECommerce.Remote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Events.Clients.SubscriberClient
{
    public static class SubscriberClientServiceExtension
    {
        public static void AddSubscriber(this IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource)
        {
            var settings = new NotificationServiceSettings();

            configuration.GetSection("Notification").Bind(settings);

            var options = Options.Create(settings);

            var remoteServiceSettings = new RemoteServiceSettings();
            configuration.GetSection("Notification:Subscriber").Bind(remoteServiceSettings);

            RegisterSubscriber(services, remoteServiceSettings, options, loggerFactory, diagnosticSource);
        }

        public static void AddSubscriber(this IServiceCollection services, RemoteServiceSettings remoteServiceSettings, IOptions<NotificationServiceSettings> options, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource, HttpClient remoteHttpClient = null)
        {
            RegisterSubscriber(services, remoteServiceSettings, options, loggerFactory, diagnosticSource);
        }

        private static void RegisterSubscriber(IServiceCollection services, RemoteServiceSettings remoteServiceSettings,
            IOptions<NotificationServiceSettings> options, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource)
        {
            var settings = options.Value;

            if (!remoteServiceSettings.IsLocal) return;

            var serviceProvider = services.BuildServiceProvider();

            var eventChannelRepository = serviceProvider.GetService<EventChannelRepository>();

            if (eventChannelRepository == null)
            {
                eventChannelRepository = new EventChannelRepository(settings.Repository.ProviderAssembly,
                    new ConnectionOptions
                    {
                        Provider = settings.Repository.ProviderType,
                        ConnectionString = settings.Repository.Channel,
                    }, loggerFactory, diagnosticSource);

                services.AddSingleton(eventChannelRepository);
            }

            var eventSubscriptionRepository = serviceProvider.GetService<EventSubscriptionRepository>();

            if (eventSubscriptionRepository == null)
            {
                eventSubscriptionRepository = new EventSubscriptionRepository(eventChannelRepository,
                    settings.Repository.ProviderAssembly,
                    new ConnectionOptions
                    {
                        Provider = settings.Repository.ProviderType,
                        ConnectionString = settings.Repository.Subscription
                    }, loggerFactory, diagnosticSource);

                services.AddSingleton(eventSubscriptionRepository);
            }

            var eventRepository = serviceProvider.GetService<EventRepository>();

            if (eventRepository == null)
            {
                eventRepository = new EventRepository(eventChannelRepository, settings.Repository.ProviderAssembly,
                    new ConnectionOptions
                    {
                        Provider = settings.Repository.ProviderType,
                        ConnectionString = settings.Repository.Events
                    }, loggerFactory, diagnosticSource);

                services.AddSingleton(eventRepository);
            }

            var subscriber = new SubscriberClientService(eventChannelRepository,
                eventSubscriptionRepository, eventRepository,
                options, loggerFactory, diagnosticSource);

            services.AddSingleton(subscriber);
            services.AddSingleton<IHostedService>(subscriber);
        }
    }
}