using System;
using System.Threading.Tasks;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Clients.Core
{
    public interface IPublisherClientService:IDisposable
    {
        Task<ExecutionResult<EventMessage>> Publish(string content);
    }
}
