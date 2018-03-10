using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ECommerce.Events.Models
{
    [DataContract]
    public class EventMessage
    {
        public EventMessage()
        {
            Subscription = new List<EventSubscription>();
        }

        [Key, DataMember]
        public string Key { get; set; }

        [DataMember]
        public EventChannel Channel { get; set; }

        [DataMember]
        public DateTime PublishDateTime { get; set; }

        [DataMember]
        public bool IsProcessing { get; set; }

        [DataMember]
        public bool IsProcessed { get; set; }

        [DataMember]
        public DateTime? ProcessingStartDateTime { get; set; }

        [DataMember]
        public  DateTime? ProcessedDateTime { get; set; }

        public IEnumerable<EventSubscription> Subscription { get; set; }

        public string Message { get; set; }
    }
}