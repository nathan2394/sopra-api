using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    public class AltSizeDetail : Entity
    {
        public long? RefID { get; set; }
        public int? AltSizeID { get; set; }
        public int? ThermosID { get; set; }
    }
}
