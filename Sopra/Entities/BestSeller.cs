using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "BestSellers")]
    public class BestSeller : Entity
    {
        public long RefID { get; set; }
        public string ProductName { get; set; }
        public long ObjectID { get; set; }
        public string Type { get; set; }
        public decimal Qty { get; set; }
    }
}
