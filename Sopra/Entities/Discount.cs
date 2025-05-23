using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    public class Discount : Entity
    {
        public long? RefID { get; set; }
        public decimal? Disc { get; set; }
        public long? Amount { get; set; }
        public long? AmountMax { get; set; }
        public string? Type { get; set; }
    }
}
