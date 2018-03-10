using System;
using System.IO;
using System.Threading.Tasks;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Core;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Clients.PublisherClient;
using ECommerce.Events.Data.Repositories;
using ECommerce.Remote;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.Events.Clients.Tests
{
    public class PublisherClientServiceExtensionTest : IDisposable
    {
        private bool _disposed;
        private readonly ServiceProvider _serviceProvider;
        private readonly LocalPublisherClientService _publisher;
        private readonly string _channelName;

        public PublisherClientServiceExtensionTest()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();

            var services = new ServiceCollection();

            var eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\channel_{Guid.NewGuid()}.json").FullName
                }, loggerFactory, new MyDiagnosticSource());

            services.AddSingleton(eventChannelRepository);

            var eventSubscriptionRepository =
                new EventSubscriptionRepository(eventChannelRepository, "ECommerce.Data.FileStore",
                    new ConnectionOptions
                    {
                        Provider = "FileDb",
                        ConnectionString = new FileInfo($"data\\subscription_{Guid.NewGuid()}.json").FullName
                    }, loggerFactory, new MyDiagnosticSource());

            services.AddSingleton(eventSubscriptionRepository);

            var eventRepository = new EventRepository(eventChannelRepository, "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\event_{Guid.NewGuid()}.json").FullName
                }, loggerFactory, new MyDiagnosticSource());

            services.AddSingleton(eventRepository);

            var settings =
                Options.Create(new NotificationServiceSettings
                {
                    ApplicationName = "MyApplication",
                    ServiceName = "MyService",
                    MaxLifeTimeSubscriber = 30,
                    MaxLifeTimeMessage = 30,
                    IsFifo = true,
                    CallBackType = "ECommerce.Events.Clients.Tests,ECommerce.Events.Clients.Tests.MyCallBackTest"
                });

            var publisherSetting = new RemoteServiceSettings {IsLocal = true};

            services.AddPublisher(publisherSetting, settings, loggerFactory, new MyDiagnosticSource());

            _serviceProvider = services.BuildServiceProvider();
            _publisher = _serviceProvider.GetRequiredService<LocalPublisherClientService>();

            _channelName = $"{settings.Value.ApplicationName}.{settings.Value.ServiceName}";
        }

        [Fact]
        public void ShouldPublishNotification()
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            if (!isDisposing) return;

            if (_disposed) return;

            _publisher?.Dispose();

            _disposed = true;
        }
    }
}
