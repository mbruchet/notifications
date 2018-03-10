using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ECommerce.Events.Models
{
    [DataContract]
    public class EventSubscription
    {
        [Key, DataMember]
        public string Key { get; set; }

        [DataMember]
        public EventChannel Channel { get; set; }

        [DataMember]
        public string CallBackType { get; set; }
    }
}
