using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "PromoProducts")]
    public class PromoProduct : Entity
    {
        public long RefID { get; set; }
        public long? PromoJumboId { get; set; }
        public long? PromoMixId { get; set; }
        public long? ProductsId { get; set; }
        public long? Accs1Id { get; set; }
        public long? Accs2Id { get; set; }
        public decimal? Price { get; set; }
        public decimal? Price2 { get; set; }
        public decimal? Price3 { get; set; }
        [NotMapped]
        public Promo PromoMix { get; set; }
        [NotMapped]
        public Promo PromoJumbo { get; set; }
        [NotMapped]
        public ProductDetail2 Product { get; set; }
        [NotMapped]
        public ProductDetail2 Accs1 { get; set; }
        [NotMapped]
        public ProductDetail2 Accs2 { get; set; }
    }
}
