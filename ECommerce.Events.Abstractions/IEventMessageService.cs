using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Abstractions
{
    public interface IEventMessageService
    {
        IEventRepository EventRepository { get; }

        Task<ExecutionResult<IEnumerable<EventMessage>>> GetEventsByChannelAsync(EventChannel channel);
        Task<ExecutionResult<EventMessage>> GetMessageBySubscriberAsync(EventSubscription subscription);
        Task<ExecutionResult<EventMessage>> PublishAsync(EventMessage eventMessage);
        Task<ExecutionResult<IEnumerable<EventMessage>>> RevokeExpiredMessageAsync(EventChannel channel);
        Task<ExecutionResult<EventMessage>> AssignMessageToSubscriber(EventSubscription subscription, EventMessage publishResultResult);
        Task<ExecutionResult<IEnumerable<EventMessage>>> Search(Func<EventMessage, bool> func);
    }
}