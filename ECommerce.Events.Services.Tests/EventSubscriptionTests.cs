using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Events.Data.Repositories;
using ECommerce.Events.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ECommerce.Events.Services.Tests
{
    public class EventSubscriptionTests : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private bool _disposed;

        public EventSubscriptionTests()
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddConsole();
        }

        [Fact]
        public void ShouldRegiserAChannelAndSubscribe()
        {
            //Arrange 
            var eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
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

            var channel = EventChannelRegistrationTests.AddChannel(channelService);

            //Arrange Subscription service
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

            var subscriptionResult = subscriptionService.SubscribeAsync(new EventSubscription
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString()
            }).Result;

            //Assert
            subscriptionResult.Should().NotBeNull();
            subscriptionResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void ShouldRegiserAChannelAndSubscribeThrowArgumentNull()
        {
            //Arrange 
            var eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource());

            //Arrange Subscription service
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

            Action comparison = () =>
            {
                subscriptionService.SubscribeAsync(null).Wait();
            };

            //Assert
            comparison.Should().Throw<ArgumentNullException>();
        }


        [Fact]
        public void ShouldRegiserAChannelAndSubscribeThrowDuplicateName()
        {
            //Arrange 
            var eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
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

            EventChannel channel;

            //Act
            var executionGetResult = channelService.GetChannelAsync(x => x.Name == "TestChannel").Result;

            executionGetResult.Should().NotBeNull();
            executionGetResult.IsSuccessful.Should().BeTrue();

            if (executionGetResult.Result == null)
            {
                var executionResult = channelService.RegisterChannelAsync(new EventChannel
                {
                    IsFifo = true,
                    Key = Guid.NewGuid().ToString(),
                    Name = "TestChannel",
                    MaxLifeTimeMessage = 30,
                    MaxLifeTimeSubscriber = 30
                }).Result;

                executionResult.Should().NotBeNull();
                executionResult.IsSuccessful.Should().BeTrue();

                channel = executionResult.Result;
            }
            else
            {
                channel = executionGetResult.Result;
            }

            channel.Should().NotBeNull();

            //Arrange Subscription service
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

            Action comparison = () =>
            {
                var eventSubscription = new EventSubscription
                {
                    Channel = channel,
                    Key = Guid.NewGuid().ToString()
                };

                subscriptionService.SubscribeAsync(eventSubscription).Wait();
                subscriptionService.SubscribeAsync(eventSubscription).Wait();
            };

            //Assert
            comparison.Should().Throw<DuplicateNameException>();
        }

        [Fact]
        public void ShouldRegiserAChannelAndSubscribeThrowKeyNotFoundException()
        {
            //Arrange 
            var eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
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

            EventChannel channel;

            //Act
            var executionGetResult = channelService.GetChannelAsync(x => x.Name == "TestChannel").Result;

            executionGetResult.Should().NotBeNull();
            executionGetResult.IsSuccessful.Should().BeTrue();

            if (executionGetResult.Result == null)
            {
                var executionResult = channelService.RegisterChannelAsync(new EventChannel
                {
                    IsFifo = true,
                    Key = Guid.NewGuid().ToString(),
                    Name = "TestChannel",
                    MaxLifeTimeMessage = 30,
                    MaxLifeTimeSubscriber = 30
                }).Result;

                executionResult.Should().NotBeNull();
                executionResult.IsSuccessful.Should().BeTrue();

                channel = executionResult.Result;
            }
            else
            {
                channel = executionGetResult.Result;
            }

            channel.Should().NotBeNull();

            //Arrange Subscription service
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

            Action comparison = () =>
            {
                var eventSubscription = new EventSubscription
                {
                    Channel = new EventChannel
                    {
                        IsFifo = true,
                        Key = Guid.NewGuid().ToString(),
                        MaxLifeTimeMessage = 30,
                        MaxLifeTimeSubscriber = 30,
                        Name = "test"
                    },
                    Key = Guid.NewGuid().ToString()
                };

                subscriptionService.SubscribeAsync(eventSubscription).Wait();
            };

            //Assert
            comparison.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void ShouldRegiserAChannelThenSubscribeAndUnSubscribe()
        {
            //Arrange 
            var eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
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

            EventChannel channel;

            //Act
            var executionGetResult = channelService.GetChannelAsync(x => x.Name == "TestChannel").Result;

            executionGetResult.Should().NotBeNull();
            executionGetResult.IsSuccessful.Should().BeTrue();

            if (executionGetResult.Result == null)
            {
                var executionResult = channelService.RegisterChannelAsync(new EventChannel
                {
                    IsFifo = true,
                    Key = Guid.NewGuid().ToString(),
                    Name = "TestChannel",
                    MaxLifeTimeMessage = 30,
                    MaxLifeTimeSubscriber = 30
                }).Result;

                executionResult.Should().NotBeNull();
                executionResult.IsSuccessful.Should().BeTrue();

                channel = executionResult.Result;
            }
            else
            {
                channel = executionGetResult.Result;
            }

            channel.Should().NotBeNull();

            //Arrange Subscription service
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

            var subscriptionResult = subscriptionService.SubscribeAsync(new EventSubscription
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString()
            }).Result;

            subscriptionResult.Should().NotBeNull();
            subscriptionResult.IsSuccessful.Should().BeTrue();

            var unSubscriberesult = subscriptionService.UnSubscribeAsync(subscriptionResult.Result).Result;

            //Assert
            unSubscriberesult.Should().NotBeNull();
            unSubscriberesult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void ShouldRegiserAChannelThenSubscribeAndGetListOfSubscriptions()
        {
            //Arrange 
            var eventChannelRepository = new EventChannelRepository("ECommerce.Data.FileStore",
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

            EventChannel channel;

            //Act
            var executionGetResult = channelService.GetChannelAsync(x => x.Name == "TestChannel").Result;

            executionGetResult.Should().NotBeNull();
            executionGetResult.IsSuccessful.Should().BeTrue();

            if (executionGetResult.Result == null)
            {
                var executionResult = channelService.RegisterChannelAsync(new EventChannel
                {
                    IsFifo = true,
                    Key = Guid.NewGuid().ToString(),
                    Name = "TestChannel",
                    MaxLifeTimeMessage = 30,
                    MaxLifeTimeSubscriber = 30
                }).Result;

                executionResult.Should().NotBeNull();
                executionResult.IsSuccessful.Should().BeTrue();

                channel = executionResult.Result;
            }
            else
            {
                channel = executionGetResult.Result;
            }

            channel.Should().NotBeNull();

            //Arrange Subscription service
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

            var subscriptionResult = subscriptionService.SubscribeAsync(new EventSubscription
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString()
            }).Result;

            subscriptionResult.Should().NotBeNull();
            subscriptionResult.IsSuccessful.Should().BeTrue();

            var searchResult = subscriptionService.GetListByChannel(channel.Key).Result;

            //Assert
            searchResult.Should().NotBeNull();
            searchResult.IsSuccessful.Should().BeTrue();
            searchResult.Result.Should().HaveCount(1);
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

        public static EventSubscription AddSubscription(EventChannel channel, EventSubscriptionService subscriptionService)
        {
            var subscriptionResult = subscriptionService.SubscribeAsync(new EventSubscription
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString()
            }).Result;

            subscriptionResult.Should().NotBeNull();
            subscriptionResult.IsSuccessful.Should().BeTrue();

            return subscriptionResult.Result;
        }
    }
}
