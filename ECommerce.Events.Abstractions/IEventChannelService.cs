using System;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Services
{
    public interface IEventChannelService
    {
        IEventChannelRepository Repository { get; }
        Task<ExecutionResult<EventChannel>> GetChannelAsync(Func<EventChannel, bool> func);
        Task<ExecutionResult<EventChannel>> RegisterChannelAsync(EventChannel eventChannel);
    }
}