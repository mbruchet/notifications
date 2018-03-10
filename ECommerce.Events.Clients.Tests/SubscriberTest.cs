using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Core;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Clients.SubscriberClient;
using ECommerce.Events.Data.Repositories;
using ECommerce.Remote;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.Events.Clients.Tests
{
    public class SubscriberTest : IDisposable
    {
        private readonly EventChannelRepository _eventChannelRepository;
        private readonly SubscriberClientService _subscriber;
        private readonly EventSubscriptionRepository _eventSubscriptionRepository;
        private readonly IOptions<NotificationServiceSettings> _settings;
        private bool _disposed;

        public SubscriberTest()
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

            var eventRepository = new EventRepository(_eventChannelRepository, "ECommerce.Data.FileStore",
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

            var subscriberSettings = new RemoteServiceSettings {IsLocal = true};

            _subscriber = new SubscriberClientService(_eventChannelRepository,
                _eventSubscriptionRepository,
                eventRepository, _settings, loggerFactory, new MyDiagnosticSource());
        }

        [Fact]
        public void ShouldStartSubscription()
        {
            _subscriber.StartAsync(new CancellationToken()).Wait();

            Task.Delay(100).Wait();

            var channelName = _settings.Value.ApplicationName + "." + _settings.Value.ServiceName;

            var getChannelResult = _eventChannelRepository.SearchASingleItemAsync(x => x.Name == channelName).Result;

            getChannelResult.Should().NotBeNull();

            getChannelResult.IsSuccessful.Should().BeTrue();

            var channel = getChannelResult.Result;

            var executionResult = _eventSubscriptionRepository
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

            _eventChannelRepository?.Dispose();
            _eventSubscriptionRepository?.Dispose();
            _subscriber?.Dispose();

            _disposed = true;
        }
    }
}
