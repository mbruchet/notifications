using System.Diagnostics;
using System.Net.Http;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Core;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Data.Repositories;
using ECommerce.Remote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Events.Clients.PublisherClient
{
    public static class PublisherClientServiceExtension
    {
        private static HttpClient _httpClient;

        public static void AddPublisher(this IServiceCollection services,  IConfiguration configuration, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource)
        {
            var settings = new NotificationServiceSettings();

            configuration.GetSection("Notification").Bind(settings);

            var options = Options.Create(settings);

            var publisherSettings = new RemoteServiceSettings();
            configuration.GetSection("Notification:Publisher").Bind(publisherSettings);

            RegisterPublisher(services, publisherSettings, options, loggerFactory, diagnosticSource, settings);
        }

        public static void AddPublisher(this IServiceCollection services, RemoteServiceSettings publisherSettings, IOptions<NotificationServiceSettings> options, LoggerFactory loggerFactory, DiagnosticSource diagnosticSource)
        {
            var settings = options.Value;

            RegisterPublisher(services, publisherSettings, options, loggerFactory, diagnosticSource, settings);
        }

        public static void AddPublisher(this IServiceCollection services, RemoteServiceSettings publisherSettings, IOptions<NotificationServiceSettings> options, LoggerFactory loggerFactory, DiagnosticSource diagnosticSource, HttpClient httpClient)
        {
            var settings = options.Value;
            _httpClient = httpClient;
            RegisterPublisher(services, publisherSettings, options, loggerFactory, diagnosticSource, settings);
        }

        private static void RegisterPublisher(IServiceCollection services, RemoteServiceSettings publisherSettings, IOptions<NotificationServiceSettings> options, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource, NotificationServiceSettings settings)
        {
            var serviceProvider = services.BuildServiceProvider();

            if (publisherSettings.IsLocal)
            {
                RegisterLocalPublisher(services, publisherSettings, options, loggerFactory, diagnosticSource, settings, serviceProvider);
            }
            else
            {
                RegisterRemotePublisher(publisherSettings, services);
            }
        }

        private static void RegisterRemotePublisher(RemoteServiceSettings publisherSettings, IServiceCollection services)
        {
            services.AddSingleton<IPublisherClientService>(new RemotePublisherClientService(publisherSettings, _httpClient));
        }

        private static void RegisterLocalPublisher(IServiceCollection services, RemoteServiceSettings publisherSettings, IOptions<NotificationServiceSettings> options, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource, NotificationServiceSettings settings, ServiceProvider serviceProvider)
        {
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

            var publisher = new LocalPublisherClientService(eventChannelRepository,
                eventSubscriptionRepository, eventRepository,
                options, loggerFactory, diagnosticSource);

            services.AddSingleton<IPublisherClientService>(publisher);
        }
    }
}