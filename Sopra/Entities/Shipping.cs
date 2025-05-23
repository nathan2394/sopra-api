using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Shippings")]
    public class Shipping : Entity
    {
        public long? RefID { get; set; }
        public long? OrderID { get; set; }
        public long? ProductID { get; set; }
        public decimal? Qty { get; set; }
        public string SpkNo { get; set; }
    }
}
