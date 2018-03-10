using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Clients.SubscriberClient;
using ECommerce.Events.Data.Repositories;
using ECommerce.Remote;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.Events.Clients.Tests
{
    public class SubscriberClientServiceExtensionTestAppSettings:IDisposable
    {
        private readonly SubscriberClientService _subscriber;
        private readonly IOptions<NotificationServiceSettings> _settings;
        private readonly IServiceProvider _serviceProvider;
        private bool _disposed;

        public SubscriberClientServiceExtensionTestAppSettings()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json").Build();

            var services = new ServiceCollection();

            var settings = new NotificationServiceSettings();
            config.GetSection("Notification").Bind(settings);

            _settings = Options.Create(settings);

            var subscriberSettings = new RemoteServiceSettings();
            config.GetSection("Notification:Subscriber").Bind(subscriberSettings);

            services.AddSubscriber(subscriberSettings, _settings, loggerFactory, new MyDiagnosticSource());

            _serviceProvider = services.BuildServiceProvider();
            _subscriber = _serviceProvider.GetService<SubscriberClientService>();
        }

        [Fact]
        public void ShouldStartSubscription()
        {
            _subscriber.StartAsync(new CancellationToken()).Wait();

            Task.Delay(100).Wait();

            var channelName = _settings.Value.ApplicationName + "." + _settings.Value.ServiceName;

            var eventChannelRepository = _serviceProvider.GetRequiredService<EventChannelRepository>();

            var getChannelResult = eventChannelRepository.SearchASingleItemAsync(x => x.Name == channelName).Result;

            getChannelResult.Should().NotBeNull();

            getChannelResult.IsSuccessful.Should().BeTrue();

            var channel = getChannelResult.Result;

            var eventSubscriptionRepository = _serviceProvider.GetRequiredService<EventSubscriptionRepository>();

            var executionResult = eventSubscriptionRepository
                .GetSubscriptionsPerChannel(channel).Result;

            executionResult.Should().NotBeNull();
            executionResult.IsSuccessful.Should().BeTrue();
            executionResult.Result.Should().NotBeNull();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            if (!isDisposing) return;

            if (_disposed) return;

            _subscriber?.Dispose();
            
            _disposed = true;
        }
    }
}
