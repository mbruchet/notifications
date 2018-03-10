using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Clients.PublisherClient;
using ECommerce.Events.Clients.SubscriberClient;
using ECommerce.Events.Data.Repositories;
using ECommerce.Remote;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.Events.TestIntegrations
{
    public class StandaloneTest:IDisposable
    {
        private readonly string _channelName;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPublisherClientService _publisher;
        private readonly SubscriberClientService _subscriber;

        public StandaloneTest()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();

            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json").Build();

            var settings = new NotificationServiceSettings();
            config.GetSection("Notification").Bind(settings);

            _channelName = $"{settings.ApplicationName}.{settings.ServiceName}";

            var options = Options.Create(settings);
            var diagnostic = new MyDiagnosticSource();

            var remoteSettings = new RemoteServiceSettings {IsLocal = true};

            services.AddPublisher(remoteSettings, options, loggerFactory, diagnostic);
            services.AddSubscriber(remoteSettings, options, loggerFactory, diagnostic);

            _serviceProvider = services.BuildServiceProvider();

            _publisher = _serviceProvider.GetService<IPublisherClientService>();
            _subscriber = _serviceProvider.GetService<SubscriberClientService>();
        }

        [Fact]
        public void WithStandalone_ShouldStartSubscription()
        {
            _subscriber.StartAsync(new CancellationToken()).Wait();

            var eventChannelRepository = _serviceProvider.GetService<EventChannelRepository>();
            var eventSubscriptionRepository = _serviceProvider.GetService<EventSubscriptionRepository>();

            eventChannelRepository.Should().NotBeNull();
            eventSubscriptionRepository.Should().NotBeNull();

            Task.Delay(100).Wait();

            var getChannelResult = eventChannelRepository.SearchASingleItemAsync(x => x.Name == _channelName).Result;

            getChannelResult.Should().NotBeNull();

            getChannelResult.IsSuccessful.Should().BeTrue();

            var channel = getChannelResult.Result;

            var executionResult = eventSubscriptionRepository
                .GetSubscriptionsPerChannel(channel).Result;

            executionResult.Should().NotBeNull();
            executionResult.IsSuccessful.Should().BeTrue();
            executionResult.Result.Should().NotBeNull();
        }
        
        [Fact]
        public void WithStandalone_ShouldPublishNotification()
        {
            var publishResult = _publisher.Publish("test").Result;

            publishResult.Should().NotBeNull();
            publishResult.IsSuccessful.Should().BeTrue();

            Task.Delay(100).Wait();

            var eventRepository = _serviceProvider.GetRequiredService<EventRepository>();

            var executionResult = eventRepository.SearchAsync(e => e.Channel.Name == _channelName).Result;

            executionResult.Should().NotBeNull();
            executionResult.IsSuccessful.Should().BeTrue();
            executionResult.Result.Should().NotBeNull();
        }

         [Fact]
        public void WithStandalone_WhenSubscribeThenPublishNotification_ShouldRaiseCallBack()
        {
            // arrange
            WithStandalone_ShouldStartSubscription();
            WithStandalone_ShouldPublishNotification();

            // Act 
            var notification = MyCallBackCache.Instance.Get();

            // Assert
            notification.Should().NotBeNull();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                //clean up
            }
        }
    }
}
