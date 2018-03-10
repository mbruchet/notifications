using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Abstractions
{
    public interface IEventSubscriptionService
    {
        IEventSubscriptionRepository Repository { get; }

        Task<ExecutionResult<EventSubscription>> SubscribeAsync(EventSubscription eventSubscription);
        Task<ExecutionResult<IEnumerable<EventSubscription>>> GetListSubscribersForChannel(EventChannel channel);
        Task<ExecutionResult<EventSubscription>> UnSubscribeAsync(EventSubscription subscription);
    }
}