using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ECommerce.Events.Services
{
    public class EventLiveStreamerService
    {
        private readonly DiagnosticSource _diagnosticSource;
        private readonly IEventChannelService _eventChannelRepository;
        private readonly IEventSubscriptionService _eventSubscriptionService;
        private readonly IEventMessageService _eventMessageService;

        private static readonly ConcurrentDictionary<string, IMessageCallBack> CallBacks = new ConcurrentDictionary<string, IMessageCallBack>();
        private readonly ILogger<EventLiveStreamerService> _logger;

        public EventLiveStreamerService(IEventChannelRepository eventChannelRepository,
            IEventSubscriptionRepository eventSubscriptionRepository, 
            IEventRepository eventMessageRepository, ILoggerFactory loggerFactory, 
            DiagnosticSource diagnosticSource)
        {
            _diagnosticSource = diagnosticSource;
            _eventChannelRepository = new EventChannelService(eventChannelRepository);
            _eventSubscriptionService = new EventSubscriptionService(eventSubscriptionRepository);
            _eventMessageService = new EventMessageService(eventMessageRepository);

            _logger = loggerFactory.CreateLogger<EventLiveStreamerService>();
        }

        public async Task<ExecutionResult<EventChannel>> GetChannel(string name)
        {
            if (_diagnosticSource.IsEnabled($"{nameof(GetChannel)}"))
                _diagnosticSource.Write($"{nameof(GetChannel)}", name);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                return await _eventChannelRepository.GetChannelAsync(c => c.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(GetChannel)} {name} failed");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogTrace($"{nameof(GetChannel)}:Ends in {stopwatch.Elapsed}");
            }
        }

        public async Task<ExecutionResult<EventChannel>> CreateChannel(string name, bool fifo, int maxLifeTimeSubscriber, int maxLifeTimeMessage)
        {
            if(_diagnosticSource.IsEnabled($"{nameof(CreateChannel)}"))
                _diagnosticSource.Write($"{nameof(CreateChannel)}", name);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                return await _eventChannelRepository.RegisterChannelAsync(new EventChannel
                {
                    Name = name,
                    Key = Guid.NewGuid().ToString(),
                    MaxLifeTimeMessage = maxLifeTimeMessage,
                    MaxLifeTimeSubscriber = maxLifeTimeSubscriber,
                    IsFifo = fifo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(CreateChannel)} {name} failed");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogTrace($"{nameof(CreateChannel)}:Ends in {stopwatch.Elapsed}");
            }
        }

        public async Task<ExecutionResult<EventSubscription>> Subscribe(string channelName, IMessageCallBack callBackAction)
        {
            if(_diagnosticSource.IsEnabled("Subscribe"))
                _diagnosticSource.Write("Subscribe", channelName);

            var getChannelResult = await _eventChannelRepository.GetChannelAsync(s => s.Name == channelName);

            if (!getChannelResult.IsSuccessful) return BadResult<EventSubscription>();

            var channel = getChannelResult.Result;

            if (channel == null) return BadResult<EventSubscription>();

            var eventSubscriptionResult = await _eventSubscriptionService.SubscribeAsync(new EventSubscription
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                CallBackType = $"{callBackAction.GetType().Assembly.FullName},{callBackAction.GetType().FullName}"
            });

            if (!eventSubscriptionResult.IsSuccessful) return BadResult<EventSubscription>();

            var getEventMessageResult =
                await _eventMessageService.GetMessageBySubscriberAsync(eventSubscriptionResult.Result);

            if (!getEventMessageResult.IsSuccessful) return BadResult<EventSubscription>();

            if (getEventMessageResult.Result != null)
                callBackAction.Invoke(getEventMessageResult.Result).Wait();

            CallBacks.TryAdd(eventSubscriptionResult.Result.Key, callBackAction);

            return new ExecutionResult<EventSubscription>(true, eventSubscriptionResult.Result);
        }

        public async Task<ExecutionResult<EventSubscription>> Subscribe(string channelName, string callBackActionType)
        {
            if (_diagnosticSource.IsEnabled("Subscribe"))
                _diagnosticSource.Write("Subscribe", channelName);

            var getChannelResult = await _eventChannelRepository.GetChannelAsync(s => s.Name == channelName);

            if (!getChannelResult.IsSuccessful) return BadResult<EventSubscription>();

            var channel = getChannelResult.Result;

            if (channel == null) return BadResult<EventSubscription>();

            var eventSubscriptionResult = await _eventSubscriptionService.SubscribeAsync(new EventSubscription
            {
                Channel = channel,
                Key = Guid.NewGuid().ToString(),
                CallBackType = callBackActionType
            });

            if (!eventSubscriptionResult.IsSuccessful) return BadResult<EventSubscription>();

            var getEventMessageResult =
                await _eventMessageService.GetMessageBySubscriberAsync(eventSubscriptionResult.Result);

            if (!getEventMessageResult.IsSuccessful) return BadResult<EventSubscription>();

            if(getEventMessageResult.IsSuccessful && getEventMessageResult.Result == null)
                return new ExecutionResult<EventSubscription>(true, eventSubscriptionResult.Result);

            var eventMessage = getEventMessageResult.Result;
            var subscription = eventSubscriptionResult.Result;

            InvokeCallBack(callBackActionType, eventMessage, subscription);

            return new ExecutionResult<EventSubscription>(true, eventSubscriptionResult.Result);
        }

        public void InvokeCallBack(string callBackActionType, EventMessage eventMessage, EventSubscription subscription)
        {
            var callBack = GetCallBack(callBackActionType);

            callBack.Invoke(eventMessage).Wait();

            eventMessage.IsProcessed = true;
            eventMessage.IsProcessing = true;
            eventMessage.ProcessedDateTime = DateTime.Now;
            eventMessage.ProcessingStartDateTime = DateTime.Now;

            _eventMessageService.EventRepository.UpdateAsync(eventMessage).Wait();

            if(!CallBacks.ContainsKey(subscription.Key))
                CallBacks.TryAdd(subscription.Key, callBack);
        }

        public async Task<ExecutionResult<EventMessage>> Publish(string channelName, string content)
        {
            //get a channel if not exist, return false
            var getChannelResult = await _eventChannelRepository.GetChannelAsync(s => s.Name == channelName);

            if (!getChannelResult.IsSuccessful) return BadResult<EventMessage>();

            var channel = getChannelResult.Result;

            if (channel == null) return BadResult<EventMessage>();

            //publish a content if an error occurred return it
            var publishResult = await _eventMessageService.PublishAsync(new EventMessage
            {
                Key = Guid.NewGuid().ToString(),
                Channel = channel,
                PublishDateTime = DateTime.Now,
                Message = content
            });

            return publishResult;            
        }
        
        private static IMessageCallBack GetCallBack(string callBackTypeName)
        {
            IMessageCallBack callBack = null;

            var parts = callBackTypeName.Split(',');

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.Equals(parts[0]));

            if (assembly == null)
            {
                assembly = Assembly.Load(parts[0]);
            }

            var callBackType = assembly.GetType(parts[1]);

            if (callBackType == null) return null;
            {
                callBack = (IMessageCallBack) Activator.CreateInstance(callBackType);
            }

            return callBack;
        }

        private static ExecutionResult<T> BadResult<T>() where T : class, new() => new ExecutionResult<T>(false, null);

        public async Task UnSubscribe(CancellationToken cancellationToken, EventSubscription subscription)
        {
            if (_diagnosticSource.IsEnabled("Subscribe"))
                _diagnosticSource.Write("UnSubscribe", subscription.Key);

            await _eventSubscriptionService.UnSubscribeAsync(subscription);
        }

        public async Task<ExecutionResult<EventMessage>> GetMessage(EventSubscription subscription)
        {
            if (_diagnosticSource.IsEnabled("GetMessage"))
                _diagnosticSource.Write("GetMessage", subscription.Key);

            var getChannelResult = await _eventChannelRepository.GetChannelAsync(x => x.Name == subscription.Channel.Name);

            if (!getChannelResult.IsSuccessful) return new ExecutionResult<EventMessage>(false, null);

            var getMessageResult = await _eventMessageService.GetEventsByChannelAsync(getChannelResult.Result);

            if (!getMessageResult.IsSuccessful) return new ExecutionResult<EventMessage>(false, null);

            var message = getMessageResult.Result.FirstOrDefault(x=>!x.IsProcessed && !x.IsProcessing);

            if(message == null) return new ExecutionResult<EventMessage>(false, null);

            var executionResult = await _eventMessageService.AssignMessageToSubscriber(subscription, message);

            return executionResult.IsSuccessful
                ? new ExecutionResult<EventMessage>(true, message)
                : new ExecutionResult<EventMessage>(false, null);
        }
    }
}
