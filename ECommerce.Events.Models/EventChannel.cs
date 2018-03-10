using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ECommerce.Events.Models
{
    [DataContract]
    public class EventChannel
    {
        [Key, DataMember]
        public string Key { get; set; }

        [DataMember]
        public int MaxLifeTimeMessage { get; set; }

        [DataMember]
        public int MaxLifeTimeSubscriber { get; set; }

        [DataMember]
        public bool IsFifo { get; set; }

        [DataMember]
        public string Name { get; set; }
    }
}
