using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Models;
using ECommerce.Events.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BackgroundService = Microsoft.Extensions.Hosting.BackgroundService;

namespace ECommerce.Events.Clients.SubscriberClient
{
    public class SubscriberClientService : BackgroundService
    {
        private readonly ILogger<SubscriberClientService> _logger;
        private readonly NotificationServiceSettings _settings;
        private readonly EventLiveStreamerService _eventLiveStreamer;
        private EventSubscription _subscription;
        private readonly string _channelName;

        public SubscriberClientService(IEventChannelRepository eventChannelRepository,
            IEventSubscriptionRepository eventSubscriptionRepository,
            IEventRepository eventRepository, IOptions<NotificationServiceSettings> settings,
            ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource)
        {
            _logger = loggerFactory.CreateLogger<SubscriberClientService>();
            _settings = settings.Value;

            _eventLiveStreamer = new EventLiveStreamerService(eventChannelRepository, eventSubscriptionRepository, eventRepository, loggerFactory, diagnosticSource);

            _channelName = $"{_settings.ApplicationName}.{_settings.ServiceName}";

            var getChannelResult = _eventLiveStreamer.GetChannel(_channelName).Result;

            if (!getChannelResult.IsSuccessful || getChannelResult.Result == null)
                _eventLiveStreamer.CreateChannel(_channelName, _settings.IsFifo, _settings.MaxLifeTimeSubscriber, _settings.MaxLifeTimeMessage).Wait();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"Subscriber service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {

                if (_eventLiveStreamer != null && _subscription != null)
                {
                    _logger.LogDebug($"Subscriber Check if a message is published.");

                    var getMessageResult = await _eventLiveStreamer.GetMessage(_subscription);

                    if (getMessageResult.IsSuccessful && getMessageResult.Result != null)
                    {
                        _logger.LogDebug($"Subscriber A message is published invoke the callback.");
                        _eventLiveStreamer.InvokeCallBack(_subscription.CallBackType, getMessageResult.Result, _subscription);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.MaxLifeTimeSubscriber), stoppingToken);
            }

            _logger.LogDebug($"Subscriber service is stopping.");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_eventLiveStreamer != null)
            {
                var executionResult = await _eventLiveStreamer.Subscribe(_channelName, _settings.CallBackType);

                if (executionResult.IsSuccessful)
                {
                    _subscription = executionResult.Result;
                    await Task.Factory.StartNew(() => ExecuteAsync(cancellationToken), cancellationToken);
                }
                else
                    throw new PlatformNotSupportedException();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_eventLiveStreamer != null)
                await _eventLiveStreamer.UnSubscribe(cancellationToken, _subscription);
        }
    }
}