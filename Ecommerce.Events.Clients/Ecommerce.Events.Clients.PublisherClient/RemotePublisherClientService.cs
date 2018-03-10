using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Core;
using ECommerce.Events.Clients.Core;
using ECommerce.Events.Models;
using ECommerce.Remote;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;

namespace ECommerce.Events.Clients.PublisherClient
{
    public class RemotePublisherClientService : IPublisherClientService
    {
        private readonly RemoteServiceSettings _publisherSettings;
        private readonly HttpClient _httpClient;
        private readonly string _authenticationToken;
        private readonly string _tokenType;

        public RemotePublisherClientService(RemoteServiceSettings publisherSettings, HttpClient httpClient = null, string authenticationToken = "", string tokenType = "")
        {
            _publisherSettings = publisherSettings;
            _httpClient = httpClient;
            _authenticationToken = authenticationToken;
            _tokenType = tokenType;
        }

        public void Dispose()
        {
            // clean up
        }

        public async Task<ExecutionResult<EventMessage>> Publish(string content)
        {
            var client = _httpClient ?? new HttpClient() {BaseAddress = new Uri(_publisherSettings.Uri)};

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (_publisherSettings.Authentication?.AuthenticationType is AuthenticationTypeEnum.Basic)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    $"{_publisherSettings.Authentication.UserName}:{_publisherSettings.Authentication.Password}");
            }
            else if (_publisherSettings.Authentication?.AuthenticationType is AuthenticationTypeEnum.Jwt)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authenticationToken);
            }
            else
            {
                //no auhentication
            }

            var response = await client.PostAsync("api/publisher", new StringContent(content, Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode
                ? new ExecutionResult<EventMessage>(true,
                    JsonConvert.DeserializeObject<EventMessage>(await response.Content.ReadAsStringAsync()))
                : new ExecutionResult<EventMessage>(false, null);
        }
    }
}
