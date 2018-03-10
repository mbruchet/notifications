using ECommerce.Events.Models;

namespace ECommerce.Events.Sample.RemoteSubscriber
{
    public class MyCallBackCache
    {
        private static MyCallBackCache _instance;
        private EventMessage _message;

        public static MyCallBackCache Instance
        {
            get
            {
                _instance = _instance ?? new MyCallBackCache();
                return _instance;
            }
        }

        private MyCallBackCache() { }

        public EventMessage Get()
        {
            return _message;
        }

        public void Set(EventMessage message)
        {
            _message = message;
        }
    }
}