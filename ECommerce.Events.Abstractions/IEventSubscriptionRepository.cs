using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Abstractions
{
    public interface IEventSubscriptionRepository
    {
        Task<ExecutionResult<EventSubscription>> AddAsync(EventSubscription eventSubscription);
        Task<ExecutionResult<EventSubscription>> RemoveAsync(EventSubscription subscription);
        Task<ExecutionResult<IEnumerable<EventSubscription>>> SearchAsync(Func<EventSubscription, bool> func);
        IEventChannelRepository ChannelRepository { get; }
        Task<ExecutionResult<IEnumerable<EventSubscription>>> GetSubscriptionsPerChannel(EventChannel channel);
    }
}