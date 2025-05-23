using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    public class PromoMixDetail
    {
        public string? ProductName { get; set; }
        public string? Accs1Name { get; set; }
        public string? Accs2Name { get; set; }
        public long? PromoMixId { get; set; }
        public decimal? Price { get; set; }
        public int? Qty { get; set; }
        public decimal? Price2 { get; set; }
        public int? Qty2 { get; set; }
        public decimal? Price3 { get; set; }
        public int? Qty3 { get; set; }
        [NotMapped]
        public Promo Promo { get; set; }
    }
}
