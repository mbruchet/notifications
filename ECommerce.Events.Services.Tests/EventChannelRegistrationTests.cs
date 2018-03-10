using System;
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
    public class EventChannelRegistrationTests:IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private bool _disposed;

        public EventChannelRegistrationTests()
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddConsole();
        }

        [Fact]
        public void RegisterANewChannel_ShouldReturnTrue()
        {
            //Arrange 
            var channelService = new EventChannelService
                (
                new EventChannelRepository("ECommerce.Data.FileStore",
                new ConnectionOptions
                {
                    Provider = "FileDb",
                    ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                },
                _loggerFactory, new MyDiagnosticSource())
            );

            //Act
            var executionResult = channelService.RegisterChannelAsync(new EventChannel
            {
                IsFifo = true,
                Key = Guid.NewGuid().ToString(),
                Name = "TestChannel",
                MaxLifeTimeMessage = 30,
                MaxLifeTimeSubscriber = 30
            }).Result;

            //Assert
            executionResult.Should().NotBeNull();
            executionResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void RegisterANewChannel_ShouldThrowArgumentNullException()
        {
            //Arange 
            var channelService = new EventChannelService
            (
                new EventChannelRepository("ECommerce.Data.FileStore",
                    new ConnectionOptions
                    {
                        Provider = "FileDb",
                        ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                    },
                    _loggerFactory, new MyDiagnosticSource())
            );

            //Act
            Action comparison = () => { channelService.RegisterChannelAsync(null).Wait(); };

            //Assert
            comparison.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RegisterANewChannel_ShouldThrowDuplicateKeyException()
        {
            //Arange 
            var channelService = new EventChannelService
            (
                new EventChannelRepository("ECommerce.Data.FileStore",
                    new ConnectionOptions
                    {
                        Provider = "FileDb",
                        ConnectionString = new FileInfo($"data\\data_{Guid.NewGuid()}.json").FullName
                    },
                    _loggerFactory, new MyDiagnosticSource())
            );

            //Act
            Action comparison = () =>
            {
                var channel = new EventChannel
                {
                    IsFifo = true,
                    Key = Guid.NewGuid().ToString(),
                    Name = "TestChannel",
                    MaxLifeTimeMessage = 30,
                    MaxLifeTimeSubscriber = 30
                };

                channelService.RegisterChannelAsync(channel).Wait();

                channelService.RegisterChannelAsync(channel).Wait();
            };

            //Assert
            comparison.Should().Throw<DuplicateNameException>();

        }

        public static EventChannel AddChannel(IEventChannelService channelService, int timeout = 30)
        {
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
                    MaxLifeTimeMessage = timeout,
                    MaxLifeTimeSubscriber = timeout
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

            return channel;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if(_disposed) return;

            if (!disposing) return;

            _loggerFactory.Dispose();
            _disposed = true;
        }
    }
}