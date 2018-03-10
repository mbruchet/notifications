using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Services
{
    public class EventMessageService : IEventMessageService
    {
        public IEventRepository EventRepository { get; }

        public EventMessageService(IEventRepository repository)
        {
            EventRepository = repository;
        }

        public async Task<ExecutionResult<EventMessage>> PublishAsync(EventMessage eventMessage)
        {
            if (eventMessage == null) throw new ArgumentNullException(nameof(eventMessage));
            eventMessage.PublishDateTime = DateTime.Now;
            return await EventRepository.AddAsync(eventMessage);
        }

        public async Task<ExecutionResult<IEnumerable<EventMessage>>> GetEventsByChannelAsync(EventChannel channel)
        {
            return await EventRepository.SearchAsync(e => e.Channel.Key == channel.Key);
        }

        public async Task<ExecutionResult<EventMessage>> GetMessageBySubscriberAsync(EventSubscription subscription)
        {
            //get all message for a channel

            var searchMessagesResult = await EventRepository.SearchAsync(m =>
                (m.Channel.Key == subscription.Channel.Key && 
                 (!m.Channel.IsFifo || !m.IsProcessing)) ||
                m.Subscription.Any(s=>s.Key == subscription.Key));

            if(searchMessagesResult?.IsSuccessful != true && searchMessagesResult?.Result == null) return  new ExecutionResult<EventMessage>(false, null);

            var message = searchMessagesResult.Result.FirstOrDefault();

            if (message == null) return new ExecutionResult<EventMessage>(true, null);

            return await AssignMessageToSubscriber(subscription, message);
        }

        public async Task<ExecutionResult<IEnumerable<EventMessage>>> RevokeExpiredMessageAsync(EventChannel channel)
        {
            var searchEvents = await EventRepository.SearchAsync(e => e.Channel.Key == channel.Key && !e.IsProcessing);

            if (!searchEvents.IsSuccessful) return new ExecutionResult<IEnumerable<EventMessage>>(false, null);

            var events = searchEvents.Result
                .Where(x => DateTime.Now.Subtract(x.PublishDateTime).TotalSeconds > channel.MaxLifeTimeMessage)
                .ToList();

            if (!events.Any()) return new ExecutionResult<IEnumerable<EventMessage>>(false, null);

            foreach (var eventMessage in events)
            {
                EventRepository.RemoveAsync(eventMessage).Wait();
            }

            return new ExecutionResult<IEnumerable<EventMessage>>(true, events);
        }

        public async Task<ExecutionResult<EventMessage>> AssignMessageToSubscriber(EventSubscription subscription, EventMessage message)
        {
            message.IsProcessing = true;
            message.ProcessingStartDateTime = DateTime.Now;

            message.Subscription = message.Subscription.Any() ? new List<EventSubscription>(message.Subscription){subscription} : new List<EventSubscription>() { subscription };

            return await EventRepository.UpdateAsync(message);
        }

        public async Task<ExecutionResult<IEnumerable<EventMessage>>> Search(Func<EventMessage, bool> func)
        {
            return await EventRepository.SearchAsync(func);
        }

        public async Task<ExecutionResult<EventMessage>> UnProcessingEventAsync(EventMessage message)
        {
            message.IsProcessing = false;
            message.IsProcessed = false;
            message.ProcessingStartDateTime = null;
            message.ProcessedDateTime = null;
            message.Subscription = null;

            return await EventRepository.UpdateAsync(message);
        }
    }
}
