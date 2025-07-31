using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Deposits")]
    public class Deposit : Entity
    {
        public long? RefID { get; set; }
        public long? ObjectID { get; set; }
        public long? CustomersID { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? TransDate { get; set; }
        public DateTime? DateIn { get; set; }
    }
}
