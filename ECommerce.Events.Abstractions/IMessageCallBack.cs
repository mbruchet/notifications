using System.Threading.Tasks;
using ECommerce.Events.Models;

namespace ECommerce.Events.Abstractions
{
    public interface IMessageCallBack
    {
        Task Invoke(EventMessage result);
    }
}