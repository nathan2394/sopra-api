using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "PromoQuantities")]
    public class PromoQuantity : Entity
    {
        public long RefID { get; set; }
        public long? PromoJumboId { get; set; }
        public long? PromoMixId { get; set; }
        public int? MinQuantity { get; set; }
        public decimal? Price { get; set; }
        public int? Level{ get; set; }
        [NotMapped]
        public Promo PromoMix { get; set; }
        [NotMapped]
        public Promo PromoJumbo { get; set; }
    }
}
