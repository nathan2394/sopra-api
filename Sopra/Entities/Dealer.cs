using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "Dealers")]
    public class Dealer : Entity
    {
        public long RefID { get; set; }
        public string? Tier { get; set; }
        public decimal? DiscBottle { get; set; }
        public decimal? DiscThermo { get; set; }
    }
}
