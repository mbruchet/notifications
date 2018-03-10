using System.Diagnostics;
using Ecommerce.Data.RepositoryStore;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Events.Data.Repositories
{
    public class EventChannelRepository: RepositoryStoreFactory<EventChannel>, IEventChannelRepository
    {
        public EventChannelRepository(string assembly, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource) : base(assembly, connectionOptions, loggerFactory, diagnosticSource)
        {
        }
    }
}
