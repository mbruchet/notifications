using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ECommerce.Events.Data.Repositories
{
    public class EventSubscriptionRepository : RepositoryStoreFactory<EventSubscription>, IEventSubscriptionRepository
    {
        public IEventChannelRepository ChannelRepository { get; }
        public string Assembly { get; }
        public ConnectionOptions Options { get; }
        public ILoggerFactory Logging { get; }
        public DiagnosticSource Diagnostic { get; }

        public EventSubscriptionRepository(EventChannelRepository repository, string assembly, ConnectionOptions connectionOptions, ILoggerFactory loggerLogging, DiagnosticSource diagnosticDiagnostic) : base(assembly, connectionOptions, loggerLogging, diagnosticDiagnostic)
        {
            ChannelRepository = repository;
            Assembly = assembly;
            Options = connectionOptions;
            Logging = loggerLogging;
            Diagnostic = diagnosticDiagnostic;
            BeforeInsert = OnInsert;
        }

        private void OnInsert(EventSubscription obj)
        {
            if (obj.Channel == null)
                throw new ArgumentNullException(nameof(obj), "Channel is required");

            if (string.IsNullOrEmpty(obj.Channel.Key))
                throw new ArgumentNullException(nameof(obj), "Channel key is required");

            var getEventChannelResult = ChannelRepository.SearchASingleItemAsync(x => x.Key == obj.Channel.Key).Result;

            if (getEventChannelResult.IsSuccessful && getEventChannelResult.Result != null)
                return;

            throw new KeyNotFoundException($"can not find channel {obj.Channel.Name}, please create it before to use it");
        }

        public async Task<ExecutionResult<IEnumerable<EventSubscription>>> GetSubscriptionsPerChannel(EventChannel channel)
        {
            return await SearchAsync(s => s.Channel.Key == channel.Key);
        }
    }
}