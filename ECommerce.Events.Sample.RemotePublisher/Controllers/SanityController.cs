using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Events.Abstractions;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ECommerce.Events.Sample.RemotePublisher.Controllers
{
    [Route("api/[controller]")]
    public class SanityController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private readonly NotificationServiceSettings _notificationServiceSettings;
        private readonly string _channelName;

        public SanityController(IEventRepository eventRepository, NotificationServiceSettings notificationServiceSettings)
        {
            _eventRepository = eventRepository;
            _notificationServiceSettings = notificationServiceSettings;
            _channelName = $"{_notificationServiceSettings.ApplicationName}.{_notificationServiceSettings.ServiceName}";
        }

        // GET: api/<controller>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var searchEventResult = await _eventRepository.SearchAsync(e => e.Channel.Key == _channelName);

            return searchEventResult.IsSuccessful && searchEventResult.Result != null
                ? (IActionResult) Ok(searchEventResult.Result)
                : NotFound();
        }
    }
}
