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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.Events.Clients.Tests
{
    public class PublisherTest : IDisposable
    {
        private readonly EventChannelRepository _eventChannelRepository;
        private readonly IPublisherClientService _publisher;
        private readonly EventSubscriptionRepository _eventSubscriptionRepository;
        private readonly IOptions<NotificationServiceSettings> _settings;
        private bool _disposed;
        private EventRepository _eventRepository;

        public PublisherTest()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();

            _eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\channel_{Guid.NewGuid()}.json").FullName
                }, loggerFactory, new MyDiagnosticSource());

            _eventSubscriptionRepository =
                new EventSubscriptionRepository(_eventChannelRepository, "ECommerce.Data.FileStore",
                    new ConnectionOptions
                    {
                        Provider = "FileDb",
                        ConnectionString = new FileInfo($"data\\subscription_{Guid.NewGuid()}.json").FullName
                    }, loggerFactory, new MyDiagnosticSource());

            _eventRepository = new EventRepository(_eventChannelRepository, "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\event_{Guid.NewGuid()}.json").FullName
                }, loggerFactory, new MyDiagnosticSource());


            _settings =
                Options.Create(new NotificationServiceSettings
                {
                    ApplicationName = "MyApplication",
                    ServiceName = "MyService",
                    MaxLifeTimeSubscriber = 30,
                    MaxLifeTimeMessage = 30,
                    IsFifo = true,
                    CallBackType = "ECommerce.Events.Clients.Tests,ECommerce.Events.Clients.Tests.MyCallBackTest"
                });

            var publisherSettings = new RemoteServiceSettings {IsLocal = true};

            _publisher = new LocalPublisherClientService(_eventChannelRepository,
                _eventSubscriptionRepository,
                _eventRepository, _settings, loggerFactory, new MyDiagnosticSource());
        }

        [Fact]
        public void ShouldPublishNotification()
        {
            var channelName = _settings.Value.ApplicationName + "." + _settings.Value.ServiceName;

            var publishResult = _publisher.Publish("test").Result;

            publishResult.Should().NotBeNull();
            publishResult.IsSuccessful.Should().BeTrue();

            Task.Delay(100).Wait();

            var executionResult = _eventRepository.SearchAsync(e => e.Channel.Name == channelName).Result;

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

            _eventChannelRepository?.Dispose();
            _eventSubscriptionRepository?.Dispose();
            _publisher?.Dispose();

            _disposed = true;
        }
    }
}
