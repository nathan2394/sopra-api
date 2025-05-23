using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    public class Filter : Entity
    {
        public long RefID { get; set; }
        public long? CategoryID { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public string? Type { get; set; }
    }
}
