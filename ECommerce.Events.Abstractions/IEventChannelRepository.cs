using System;
using System.Threading.Tasks;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Abstractions
{
    public interface IEventChannelRepository
    {
        Task<ExecutionResult<EventChannel>> AddAsync(EventChannel eventChannel);
        Task<ExecutionResult<EventChannel>> SearchASingleItemAsync(Func<EventChannel, bool> func);
    }
}