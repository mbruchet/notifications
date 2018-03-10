using System;
using System.IO;
using System.Threading.Tasks;
using ECommerce.Core;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Clients.PublisherClient;
using ECommerce.Events.Data.Repositories;
using ECommerce.Remote;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.Events.Clients.Tests
{
    public class PublisherClientServiceExtensionTestAppSettings : IDisposable
    {
        private bool _disposed;

        private readonly ServiceProvider _serviceProvider;
        private readonly LocalPublisherClientService _publisher;
        private readonly string _channelName;

        public PublisherClientServiceExtensionTestAppSettings()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();

            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json").Build();

            var settings = new NotificationServiceSettings();
            config.GetSection("Notification").Bind(settings);

            var publisherSettings = new RemoteServiceSettings();
            config.GetSection("Notification:Publisher").Bind(publisherSettings);

            _channelName = $"{settings.ApplicationName}.{settings.ServiceName}";

            services.AddPublisher(publisherSettings, Options.Create(settings), loggerFactory, new MyDiagnosticSource());

            _serviceProvider = services.BuildServiceProvider();
            _publisher = _serviceProvider.GetService<LocalPublisherClientService>();
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
