using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Events.Data.Repositories;
using ECommerce.Events.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ECommerce.Events.Services.Tests
{
    public class EventMessageTest : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private bool _disposed;

        public EventMessageTest()
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddConsole();
        }

        [Fact]
        public void ShouldRegisterAChannelAddASubscriptionAndPublishAnEvent()
        {
            //Arrange
            AddSubscription(out var channel, out var subscriptionService);

            var eventMessageService = new EventMessageService(new EventRepository(subscriptionService.Repository.ChannelRepository, "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            //Act
            var resultPublish = eventMessageService.PublishAsync(new EventMessage
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                PublishDateTime = DateTime.Now
            }).Result;

            //Assert
            resultPublish.Should().NotBeNull();
            resultPublish.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void ShouldRegisterAChannelAddASubscriptionAndPublishAnEventThrowArgumentNull()
        {
            //Arrange
            AddSubscription(out var _, out var subscriptionService);

            var eventMessageService = new EventMessageService(new EventRepository(subscriptionService.Repository.ChannelRepository, "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            //Act
            Action comparison = () =>
            {
                eventMessageService.PublishAsync(null).Wait();
            };

            //Assert
            comparison.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ShouldRegisterAChannelAddASubscriptionAndPublishAnEventThrowDuplicateName()
        {
            //Arrange
            AddSubscription(out var channel, out var subscriptionService);

            var eventMessageService = new EventMessageService(new EventRepository(subscriptionService.Repository.ChannelRepository, "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            var resultPublish = eventMessageService.PublishAsync(new EventMessage
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                PublishDateTime = DateTime.Now
            }).Result;

            resultPublish.Should().NotBeNull();
            resultPublish.IsSuccessful.Should().BeTrue();

            //Act
            Action comparison = () =>
            {
                eventMessageService.PublishAsync(new EventMessage
                {
                    Channel = channel,
                    Key = resultPublish.Result.Key,
                    PublishDateTime = DateTime.Now
                }).Wait();
            };

            //Assert
            comparison.Should().Throw<DuplicateNameException>();
        }

        [Fact]
        public void ShouldRegisterAChannelAddASubscriptionAndPublishAnEventThrowInvalideOperation()
        {
            //Arrange
            AddSubscription(out var _, out var subscriptionService);

            var eventMessageService = new EventMessageService(new EventRepository(subscriptionService.Repository.ChannelRepository, "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            //Act
            Action comparison = () =>
            {
                try
                {
                    var resultPublish = eventMessageService.PublishAsync(new EventMessage
                    {
                        Channel = new EventChannel
                        {
                            IsFifo = true,
                            Key = Guid.NewGuid().ToString(),
                            MaxLifeTimeMessage = 30,
                            Name = "Test",
                            MaxLifeTimeSubscriber = 30
                        },
                        Key = Guid.NewGuid().ToString(),
                        PublishDateTime = DateTime.Now
                    }).Result;

                    resultPublish.Should().NotBeNull();
                    resultPublish.IsSuccessful.Should().BeTrue();
                }
                catch (AggregateException ex)
                {
                    ex.Handle((x) => throw x);
                }
            };

            //Assert
            comparison.Should().Throw<InvalidOperationException>();
        }


        [Fact]
        public void ShouldPoolInWaitingQueeUntilASubscriberArrives()
        {
            AddChannel(out var eventChannelRepository, out var channel);

            var eventMessageService = new EventMessageService(new EventRepository(eventChannelRepository,
                "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            //Act
            var resultPublish = eventMessageService.PublishAsync(new EventMessage
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                PublishDateTime = DateTime.Now
            }).Result;

            //Assert
            resultPublish.Should().NotBeNull();
            resultPublish.IsSuccessful.Should().BeTrue();

            var searchResult = eventMessageService.GetEventsByChannelAsync(channel).Result;
            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();

            var message = searchResult.Result?.FirstOrDefault();
            message.Should().NotBeNull();

            // Assert
            message?.IsProcessing.Should().BeFalse();
        }

        [Fact]
        public void ShouldAddTheMessageAndPassItDirectlyToTheSubscriber()
        {
            var subscription = AddSubscription(out var channel, out var eventSubscriptionService);

            var eventMessageService = new EventMessageService(new EventRepository(eventSubscriptionService.Repository.ChannelRepository,
                "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            //Act
            var resultPublish = eventMessageService.PublishAsync(new EventMessage
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                PublishDateTime = DateTime.Now
            }).Result;

            //Assert
            resultPublish.Should().NotBeNull();
            resultPublish.IsSuccessful.Should().BeTrue();

            var resultGetMessage = eventMessageService.GetMessageBySubscriberAsync(subscription).Result;

            resultGetMessage.Should().NotBeNull();
            resultGetMessage.IsSuccessful.Should().BeTrue();

            Task.Delay(100).Wait();

            var searchResult = eventMessageService.GetEventsByChannelAsync(channel).Result;
            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();

            var message = searchResult.Result?.FirstOrDefault();
            message.Should().NotBeNull();

            // Assert
            message?.IsProcessing.Should().BeTrue();
        }

        [Fact]
        public void ShouldAddTheMessageAndPassItToTheSubscriberAfterStarting()
        {
            //Arrange
            AddChannel(out var eventChannelRepository, out var eventChannel);

            var eventMessageService = new EventMessageService(new EventRepository(eventChannelRepository,
                "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            var resultPublish = eventMessageService.PublishAsync(new EventMessage
            {
                Channel = eventChannel,
                Key = Guid.NewGuid().ToString(),
                PublishDateTime = DateTime.Now
            }).Result;

            resultPublish.Should().NotBeNull();
            resultPublish.IsSuccessful.Should().BeTrue();

            var searchResult = eventMessageService.GetEventsByChannelAsync(eventChannel).Result;

            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();

            var message = searchResult.Result?.FirstOrDefault();
            message.Should().NotBeNull();

            message?.IsProcessing.Should().BeFalse();

            // Act
            var subscriptionService = new EventSubscriptionService
            (
                new EventSubscriptionRepository(eventChannelRepository, "ECommerce.Data.FileStore",
                    new ConnectionOptions
                    {
                        Provider = "FileDb",
                        ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                    },
                    _loggerFactory, new MyDiagnosticSource())
            );

            var subscription = EventSubscriptionTests.AddSubscription(eventChannel, subscriptionService);
            subscription.Should().NotBeNull();

            eventMessageService.GetMessageBySubscriberAsync(subscription).Wait();

            Task.Delay(100).Wait();

            searchResult = eventMessageService.GetEventsByChannelAsync(eventChannel).Result;

            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();

            message = searchResult.Result?.FirstOrDefault();

            Assert.NotNull(message);

            message.IsProcessing.Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnTheListOfMessageExpiredWithoutProcessing()
        {
            AddChannel(out var eventChannelRepository, out var channel, 1);

            var eventMessageService = new EventMessageService(new EventRepository(eventChannelRepository,
                "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            //Act
            var resultPublish = eventMessageService.PublishAsync(new EventMessage
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                PublishDateTime = DateTime.Now
            }).Result;

            //Assert
            resultPublish.Should().NotBeNull();
            resultPublish.IsSuccessful.Should().BeTrue();

            var searchResult = eventMessageService.GetEventsByChannelAsync(channel).Result;

            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();

            var message = searchResult.Result?.FirstOrDefault();
            message.Should().NotBeNull();

            // Assert
            message?.IsProcessing.Should().BeFalse();

            Task.Delay(1000).Wait();

            var revokeResult = eventMessageService.RevokeExpiredMessageAsync(channel).Result;

            revokeResult.Should().NotBeNull();
            revokeResult.IsSuccessful.Should().BeTrue();

            revokeResult.Result.Should().HaveCountGreaterOrEqualTo(1);
        }

        [Fact]
        public void ShouldAddTheMessageAndPassItToTheSubscriberRemoveTheSubscriberAndRepassItToTheSubscriberWhenItIsReconnected()
        {
            var subscription = AddSubscription(out var channel, out var eventSubscriptionService, 1);

            var eventMessageService = new EventMessageService(new EventRepository(eventSubscriptionService.Repository.ChannelRepository,
                "ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource()));

            //Act
            var resultPublish = eventMessageService.PublishAsync(new EventMessage
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                PublishDateTime = DateTime.Now
            }).Result;

            //Assert
            resultPublish.Should().NotBeNull();
            resultPublish.IsSuccessful.Should().BeTrue();

            var resultGetMessage = eventMessageService.GetMessageBySubscriberAsync(subscription).Result;

            resultGetMessage.Should().NotBeNull();
            resultGetMessage.IsSuccessful.Should().BeTrue();

            Task.Delay(100).Wait();

            var searchResult = eventMessageService.GetEventsByChannelAsync(channel).Result;
            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();

            var message = searchResult.Result?.FirstOrDefault();
            message.Should().NotBeNull();

            // Assert
            message?.IsProcessing.Should().BeTrue();

            // Remove automatically the Subscriber by wait the agent time, the message in processing should be refreeze
            Task.Delay(1000).Wait();

            eventMessageService.UnProcessingEventAsync(message).Wait();

            Task.Delay(100).Wait();

            searchResult = eventMessageService.GetEventsByChannelAsync(channel).Result;
            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();

            message = searchResult.Result?.FirstOrDefault();
            message.Should().NotBeNull();

            // Assert
            message?.IsProcessing.Should().BeFalse();
        }

        #region private methods and dispose
        private void AddChannel(out EventChannelRepository eventChannelRepository, out EventChannel channel, int timeout = 30)
        {
            //Arrange
            eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource());
            var channelService = new EventChannelService
            (
                eventChannelRepository
            );

            channel = EventChannelRegistrationTests.AddChannel(channelService, timeout);

            channel.Should().NotBeNull();
        }

        private EventSubscription AddSubscription(out EventChannel channel, out EventSubscriptionService subscriptionService, int timeout = 30)
        {
            AddChannel(out var eventChannelRepository, out channel, timeout);

            //Arrange Subscription service
            subscriptionService = new EventSubscriptionService
            (
                new EventSubscriptionRepository(eventChannelRepository, "ECommerce.Data.FileStore",
                    new ConnectionOptions
                    {
                        Provider = "FileDb",
                        ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                    },
                    _loggerFactory, new MyDiagnosticSource())
            );

            var subscription = EventSubscriptionTests.AddSubscription(channel, subscriptionService);

            return subscription;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;

            _loggerFactory.Dispose();
            _disposed = true;
        }
        #endregion
    }
}
