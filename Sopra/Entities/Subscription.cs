using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Subscriptions")]
    public class Subscription : Entity
    {
        public long RefID { get; set; }
        public long? OrdersID { get; set; }
        public int? Status { get; set; }
        public string? Type { get; set; }
        public string? SubscriptionType { get; set; }
        public DateTime? SubscriptionDate { get; set; }
        public Order Orders { get; set; }
    }
}
