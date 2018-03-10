using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Services
{
    public class EventSubscriptionService : IEventSubscriptionService
    {
        public IEventSubscriptionRepository Repository { get; }

        public EventSubscriptionService(IEventSubscriptionRepository repository)
        {
            Repository = repository;
        }

        public async Task<ExecutionResult<EventSubscription>> SubscribeAsync(EventSubscription eventSubscription)
        {
            return await Repository.AddAsync(eventSubscription);
        }

        public async Task<ExecutionResult<IEnumerable<EventSubscription>>> GetListSubscribersForChannel(EventChannel channel)
        {
            return await Repository.SearchAsync(s => s.Channel.Key == channel.Key);
        }

        public async Task<ExecutionResult<EventSubscription>> UnSubscribeAsync(EventSubscription subscription)
        {
            return await Repository.RemoveAsync(subscription);
        }

        public async Task<ExecutionResult<IEnumerable<EventSubscription>>> GetListByChannel(string channelKey)
        {
            return await Repository.SearchAsync(x => x.Channel.Key == channelKey);
        }
    }
}