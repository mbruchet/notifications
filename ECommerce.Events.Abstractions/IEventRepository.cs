using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Events.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Events.Abstractions
{
    public interface IEventRepository
    {
        Task<ExecutionResult<EventMessage>> AddAsync(EventMessage eventMessage);
        Task<ExecutionResult<IEnumerable<EventMessage>>> SearchAsync(Func<EventMessage, bool> func);
        Task<ExecutionResult<EventMessage>> UpdateAsync(EventMessage eventMessage);
        Task<ExecutionResult<EventMessage>> RemoveAsync(EventMessage eventMessage);
    }
}