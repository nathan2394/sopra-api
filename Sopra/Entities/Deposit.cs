using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Deposits")]
    public class Deposit : Entity
    {
        public long? RefID { get; set; }
        public long? CustomersID { get; set; }
        public decimal? TotalAmount { get; set; }
    }
}
