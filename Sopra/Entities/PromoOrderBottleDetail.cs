using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "PromoOrderBottleDetails")]
    public  class PromoOrderBottleDetail : Entity
    {
        public long RefID { get; set; }
        public long? OrdersId { get; set; }
        public long? PromoMixId { get; set; }
        public long? ProductMixId { get; set; }
        public long? PromoJumboId { get; set; }
        public long? ProductJumboId { get; set; }
        public int? QtyBox { get; set; }
        public int? Qty { get; set; }
        public decimal? ProductPrice { get; set; }
        public decimal? Amount { get; set; }
        public int? FlagPromo { get; set; }
        public int? Outstanding { get; set; }
        public int? QtyAcc { get; set; }
        public string Notes { get; set; }
        public Order Orders { get; set; }
        public Promo PromoMix { get; set; }
        public Promo PromoJumbo { get; set; }
        public PromoProduct ProductJumbo { get; set; }
        public PromoProduct ProductMix { get; set; }
    }
}
