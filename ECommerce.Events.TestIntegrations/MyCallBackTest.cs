﻿using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Models;

namespace ECommerce.Events.TestIntegrations
{
    public class MyCallBackTest:IMessageCallBack
    {
        public async Task Invoke(EventMessage result)
        {
            await Task.Run(() => { MyCallBackCache.Instance.Set(result); });
        }
    }
}