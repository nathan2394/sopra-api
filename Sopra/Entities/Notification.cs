using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    public class Notification : Entity
    {

        public long? RefID{ get; set; }
        public int? UserID { get; set; }
        public string? Content { get; set; }
        public string? ContentID { get; set; }
        public string? URL { get; set; }
        public bool? IsRead { get; set; }
    }
}
