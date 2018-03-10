using System.IO;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Events.Clients.Core;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ECommerce.Events.Sample.RemotePublisher.Controllers
{
    [Route("api/[controller]")]
    public class PublisherController : Controller
    {
        private readonly IPublisherClientService _publisherClientService;

        public PublisherController(IPublisherClientService publisherClientService)
        {
            _publisherClientService = publisherClientService;
        }

        // POST api/<controller>
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true))
            {
                var bodyString = reader.ReadToEnd();

                if (string.IsNullOrEmpty(bodyString))
                    return BadRequest("Body is empty");

                var result = await _publisherClientService.Publish(bodyString);
                return result.IsSuccessful ? (IActionResult)Ok(result.Result) : BadRequest();
            }
        }
    }
}