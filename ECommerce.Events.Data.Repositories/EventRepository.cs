using System;
using System.Diagnostics;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Events.Data.Repositories
{
    public class EventRepository: RepositoryStoreFactory<EventMessage>, IEventRepository
    {
        private readonly IEventChannelRepository _eventChannelRepository;

        public EventRepository(IEventChannelRepository eventChannelRepository, string assembly, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource) : base(assembly, connectionOptions, loggerFactory, diagnosticSource)
        {
            _eventChannelRepository = eventChannelRepository;
            BeforeInsert = OnInsert;
        }

        private void OnInsert(EventMessage obj)
        {
            if(obj.Channel == null)
                throw new ArgumentNullException(nameof(obj), "Channel is required");

            if (string.IsNullOrEmpty(obj.Channel.Key))
                throw new ArgumentNullException(nameof(obj), "Channel key is required");

            var getEventChannelResult =
                _eventChannelRepository.
                    SearchASingleItemAsync(x => x.Key == obj.Channel.Key).Result;

            if (getEventChannelResult.IsSuccessful && getEventChannelResult.Result != null)
                return;

            throw new InvalidOperationException($"can not find channel {obj.Channel.Name}, please create it before to use it");
        }
    }
}
