using System;
using System.IO;
using System.Threading.Tasks;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Events.CallBackActions;
using ECommerce.Events.Data.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace ECommerce.Events.Services.Tests
{
    public class EventLiveStreamerTest : IDisposable
    {
        private bool _disposed;

        private readonly ILoggerFactory _loggerFactory;
        private readonly EventChannelRepository _eventChannelRepository;
        private readonly EventSubscriptionRepository _eventSubscriptionRepository;
        private readonly EventRepository _eventMessageRepository;
        private readonly EventLiveStreamerService _eventLiveStreamer;

        public EventLiveStreamerTest()
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddConsole();

            var diagnosticSource = new MyDiagnosticSource();

            _eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource());

            _eventSubscriptionRepository = new EventSubscriptionRepository(_eventChannelRepository,
                "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource());

            _eventMessageRepository = new EventRepository(_eventChannelRepository, "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource());

            _eventLiveStreamer = new EventLiveStreamerService(_eventChannelRepository, _eventSubscriptionRepository,
                _eventMessageRepository, _loggerFactory, diagnosticSource);
        }

        [Fact]
        public void WithLiveStreamer_ShouldCreateANewChannel()
        {
            var channelCreationResult = _eventLiveStreamer.CreateChannel(name: "Test", fifo: true, maxLifeTimeSubscriber: 30,
                maxLifeTimeMessage: 30).Result;

            channelCreationResult.Should().NotBeNull();
            channelCreationResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void WithLiveStreamer_ShouldSubscribeToAChannelAndMakeCallback()
        {
            var isCallBack = false;

            var channelCreationResult = _eventLiveStreamer.CreateChannel(name: "Test", fifo: true, maxLifeTimeSubscriber: 30,
                maxLifeTimeMessage: 30).Result;

            channelCreationResult.Should().NotBeNull();
            channelCreationResult.IsSuccessful.Should().BeTrue();

            var channelSubscriptionResult = _eventLiveStreamer.Subscribe(channelName: "Test", callBackAction: new AsyncEventMethodCallBack(
                    (message) =>
                    {
                        message.Should().NotBeNull();
                        isCallBack = true;
                    })).Result;

            channelSubscriptionResult.Should().NotBeNull();
            channelSubscriptionResult.IsSuccessful.Should().BeTrue();

            var channelPublishResult = _eventLiveStreamer.Publish(channelName: "Test", content:
                JsonConvert.SerializeObject(new
                {
                    label = "test"
                })).Result;

            channelPublishResult.Should().NotBeNull();
            channelPublishResult.IsSuccessful.Should().BeTrue();

            isCallBack.Should().BeTrue();
        }

        [Fact]
        public void WithLiveStreamer_ShouldCreateAChannelPublishAndMakeCallbackOnSubscription()
        {
            var isCallBack = false;

            var channelCreationResult = _eventLiveStreamer.CreateChannel(name: "Test", fifo: true, maxLifeTimeSubscriber: 30,
                maxLifeTimeMessage: 30).Result;

            channelCreationResult.Should().NotBeNull();
            channelCreationResult.IsSuccessful.Should().BeTrue();

            var channelPublishResult = _eventLiveStreamer.Publish(channelName: "Test", content:
                JsonConvert.SerializeObject(new
                {
                    label = "test"
                })).Result;

            channelPublishResult.Should().NotBeNull();
            channelPublishResult.IsSuccessful.Should().BeTrue();

            Task.Delay(100).Wait();

            var channelSubscriptionResult = _eventLiveStreamer.Subscribe(channelName: "Test", 
                callBackAction: new AsyncEventMethodCallBack(
                    (message) =>
                    {
                        message.Should().NotBeNull();
                        isCallBack = true;
                    })).Result;

            channelSubscriptionResult.Should().NotBeNull();
            channelSubscriptionResult.IsSuccessful.Should().BeTrue();

            Task.Delay(100).Wait();

            isCallBack.Should().BeTrue();
        }


        #region dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;

            _loggerFactory.Dispose();
            _eventChannelRepository.Dispose();
            _eventSubscriptionRepository.Dispose();
            _eventMessageRepository.Dispose();
            _disposed = true;
        }
        #endregion


    }
}
