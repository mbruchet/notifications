using ECommerce.Core;
using Newtonsoft.Json;

namespace ECommerce.Events.Clients.Core
{
    [JsonObject]
    public class NotificationServiceSettings
    {
        [JsonProperty("applicationName")]
        public string ApplicationName { get; set; }
        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }
        [JsonProperty("callbackType")]
        public string CallBackType { get; set; }
        [JsonProperty("maxLifeTimeSubscriber")]
        public int MaxLifeTimeSubscriber { get; set; }
        [JsonProperty("maxLifeTimeMessage")]
        public int MaxLifeTimeMessage { get; set; }
        [JsonProperty("fifo")]
        public bool IsFifo { get; set; }
        [JsonProperty("repository")]
        public RepositorySetting Repository { get; set; }
    }

    [JsonObject]
    public class RepositorySetting
    {
        [JsonProperty("providerAssembly")]
        public string ProviderAssembly { get; set; }

        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("subscription")]
        public string Subscription { get; set; }

        [JsonProperty("events")]
        public string Events { get; set; }
    }
}
