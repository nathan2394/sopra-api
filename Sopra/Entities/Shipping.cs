using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Shippings")]
    public class Shipping : Entity
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public long? OrderID { get; set; }
        public long? ProductID { get; set; }
        public decimal? Qty { get; set; }
        public string SpkNo { get; set; }
        public DateTime? DateIn { get; set; }
    }

    public class ShippingDto
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public long? OrderID { get; set; }
        public string? ProductName { get; set; }
        public long? ProductID { get; set; }
        public decimal? Qty { get; set; }
        public string SpkNo { get; set; }
        public DateTime? DateIn { get; set; }
        public string? ProductType { get; set; }
    }
}
