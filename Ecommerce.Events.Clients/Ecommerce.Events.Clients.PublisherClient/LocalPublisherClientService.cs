using System.Diagnostics;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Models;
using ECommerce.Events.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Events.Clients.PublisherClient
{
    public class LocalPublisherClientService:IPublisherClientService
    {
        private readonly ILogger<LocalPublisherClientService> _logger;
        private readonly EventLiveStreamerService _eventLiveStreamer;
        private readonly string _channelName;
        private bool _disposed;

        public LocalPublisherClientService(IEventChannelRepository eventChannelRepository, 
            IEventSubscriptionRepository eventSubscriptionRepository, 
            IEventRepository eventRepository, IOptions<NotificationServiceSettings> options,
            ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource)
        {
            _logger = loggerFactory.CreateLogger<LocalPublisherClientService>();
            var settings = options.Value;

            _eventLiveStreamer = new EventLiveStreamerService(eventChannelRepository, eventSubscriptionRepository,
                eventRepository, loggerFactory, diagnosticSource);

            _channelName = $"{settings.ApplicationName}.{settings.ServiceName}";

            var getChannelResult = _eventLiveStreamer.GetChannel(_channelName).Result;

            if (!getChannelResult.IsSuccessful || getChannelResult.Result == null)
                _eventLiveStreamer.CreateChannel(_channelName, settings.IsFifo, settings.MaxLifeTimeSubscriber, settings.MaxLifeTimeMessage).Wait();

        }

        public async Task<ExecutionResult<EventMessage>> Publish(string content)
        {
            _logger.LogDebug("Start publish notification.");
            return await _eventLiveStreamer.Publish(_channelName, content);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && ! _disposed)
            {
                //clean up
                _disposed = true;
            }
        }
    }
}