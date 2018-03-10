using System;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;

namespace ECommerce.Events.CallBackActions
{
    public class AsyncEventMethodCallBack:IMessageCallBack  
    {
        private Action<EventMessage> _callbackAction;

        public AsyncEventMethodCallBack(Action<EventMessage> callbackAction)
        {
            _callbackAction = callbackAction;
        }

        public Task Invoke(EventMessage result)
        {
            _callbackAction.Invoke(result);
            return Task.CompletedTask;
        }
    }
}
