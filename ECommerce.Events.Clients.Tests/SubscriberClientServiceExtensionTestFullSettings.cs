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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.Events.Clients.Tests
{
    public class SubscriberClientServiceExtensionTestFullSettings:IDisposable
    {
        private readonly SubscriberClientService _subscriber;
        private readonly IOptions<NotificationServiceSettings> _settings;
        private readonly IServiceProvider _serviceProvider;
        private bool _disposed;

        public SubscriberClientServiceExtensionTestFullSettings()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();

            var services = new ServiceCollection();

            _settings =
                Options.Create(new NotificationServiceSettings
                {
                    ApplicationName = "MyApplication",
                    ServiceName = "MyService",
                    MaxLifeTimeSubscriber = 30,
                    MaxLifeTimeMessage = 30,
                    IsFifo = true,
                    CallBackType = "ECommerce.Events.Clients.Tests,ECommerce.Events.Clients.Tests.MyCallBackTest",
                    Repository = new RepositorySetting
                    {
                        ProviderAssembly = "ECommerce.Data.FileStore",
                        ProviderType = "FileDb",
                        Channel = new FileInfo($"data\\channel_{Guid.NewGuid()}.json").FullName,
                        Subscription = new FileInfo($"data\\subscription_{Guid.NewGuid()}.json").FullName,
                        Events = new FileInfo($"data\\event_{Guid.NewGuid()}.json").FullName
                    }
                });

            var subscriberSettings = new RemoteServiceSettings {IsLocal = true};

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
