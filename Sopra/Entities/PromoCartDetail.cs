using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "PromoCartDetails")]
    public class PromoCartDetail : Entity
    {
        public long? RefID { get; set; }
        public long? CartsId { get; set; }
        public long? ProductMixId { get; set; }
        public long? PromoJumboId { get; set; }
        public long? ProductJumboId { get; set; }
        public long? PromoMixId { get; set; }
        public int? QtyBox { get; set; }
        public int? Qty { get; set; }
        public decimal? ProductPrice { get; set; }
        public decimal? Amount { get; set; }
        public int? FlagPromo { get; set; }
        public int? IsCheckout { get; set; }
        public Cart Carts { get; set; }
        public Promo PromoMix { get; set; }
        public Promo PromoJumbo { get; set; }
        public PromoProduct ProductJumbo { get; set; }
        public PromoProduct ProductMix { get; set; }
    }
}
