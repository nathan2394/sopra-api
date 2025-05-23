using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Carts")]
    public class Cart : Entity
    {
        public long RefID { get; set; }
        public long? CustomersID { get; set; }
        public long? PromosID { get; set; }
        public string? TypePromo { get; set; }
        public long? Status { get; set; }
        [NotMapped]
        public List<CartDetail>? CartDetails { get; set; }
    }
}
