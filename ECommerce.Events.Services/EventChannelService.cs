using System;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Data.Repositories;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Services
{
    public class EventChannelService : IEventChannelService
    {
        public IEventChannelRepository Repository { get; }

        public EventChannelService(IEventChannelRepository repository)
        {
            Repository = repository;
        }

        public async Task<ExecutionResult<EventChannel>> RegisterChannelAsync(EventChannel eventChannel)
        {
            return await Repository.AddAsync(eventChannel);
        }

        public async Task<ExecutionResult<EventChannel>> GetChannelAsync(Func<EventChannel, bool> func)
        {
            return await Repository.SearchASingleItemAsync(func);
        }
    }
}
