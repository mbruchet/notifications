using System;
using System.IO;
using System.Threading.Tasks;
using Ecommerce.Data.RepositoryStore;
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
    public class RemotePublisherRemoteSubscriberTests : IDisposable
    {
        private readonly string _channelName;
        private readonly IPublisherClientService _publisher;
        private readonly NotificationServiceSettings _settings;
        private readonly LoggerFactory _loggerFactory;
        private readonly MyDiagnosticSource _diagnostic;

        public RemotePublisherRemoteSubscriberTests()
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddConsole();

            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings-RemotePublisher.json").Build();

            _settings = new NotificationServiceSettings();

            config.GetSection("Notification").Bind(_settings);

            _channelName = $"{_settings.ApplicationName}.{_settings.ServiceName}";

            var options = Options.Create(_settings);
            _diagnostic = new MyDiagnosticSource();

            // Arrange a local subscriber

            var testRemoteSubscriber = new TestSbuscriberHost();

            services.AddSubscriber(new RemoteServiceSettings
            {
                IsLocal = false, Uri = testRemoteSubscriber.Server.BaseAddress.ToString()
            }, options, _loggerFactory, _diagnostic, testRemoteSubscriber.Server.CreateClient());

            // Arrange a remote publisher

            //start a publisher web site
            var testHost = new TestPublisherHost();

            services.AddPublisher(new RemoteServiceSettings
            {
                IsLocal = false, Uri = testHost.Server.BaseAddress.ToString()
            }, options, _loggerFactory, _diagnostic, testHost.Server.CreateClient());

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetService<SubscriberClientService>();
            _publisher = serviceProvider.GetService<IPublisherClientService>();

        }

        private void ShouldPublishNotification()
        {
            var publishResult = _publisher.Publish("test").Result;

            publishResult.Should().NotBeNull();
            publishResult.IsSuccessful.Should().BeTrue();

            Task.Delay(100).Wait();

            var eventChannelRepository = new EventChannelRepository(_settings.Repository.ProviderAssembly,
                new ConnectionOptions
                {
                    Provider = _settings.Repository.ProviderType,
                    ConnectionString = _settings.Repository.Channel,
                }, _loggerFactory, _diagnostic);

            var eventRepository = new EventRepository(eventChannelRepository, _settings.Repository.ProviderAssembly,
                new ConnectionOptions
                {
                    Provider = _settings.Repository.ProviderType,
                    ConnectionString = _settings.Repository.Events
                }, _loggerFactory, _diagnostic);

            var executionResult = eventRepository.SearchAsync(e => e.Channel.Name == _channelName).Result;

            executionResult.Should().NotBeNull();
            executionResult.IsSuccessful.Should().BeTrue();
            executionResult.Result.Should().NotBeNull();
        }

        [Fact]
        public void WithRemoteSubscriber_WhenPublishNotification_ShouldRaiseCallBack()
        {
            // arrange
            ShouldPublishNotification();

            Task.Delay(5000).Wait();
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
