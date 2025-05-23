using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    public class PromoDetail : Entity
    {
        public long? PromoId { get; set; }
        public string? Type { get; set; }
        public long? ProductsId { get; set; }
        public long? Accs1Id { get; set; }
        public long? Accs2Id { get; set; }
        public decimal? Price { get; set; }
        public int? Qty { get; set; }
        [NotMapped]
        public ProductDetail2 Product { get; set; }
        [NotMapped]
        public ProductDetail2 Accs1 { get; set; }
        [NotMapped]
        public ProductDetail2 Accs2 { get; set; }
    }
}
