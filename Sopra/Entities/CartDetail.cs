using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "CartDetails")]
    public class CartDetail : Entity
    {
        public long RefID { get; set; }
        public long? CartsID { get; set; }
        public long? ObjectID { get; set; }
        public long? ParentID { get; set; }
        public string? Type { get; set; }
        public string? Notes { get; set; }
        public decimal? QtyBox { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Price { get; set; }
        public decimal? Amount { get; set; }
        public bool? IsCheckout { get; set; }
        public Cart Carts { get; set; }
        [NotMapped]
        public ProductDetail2? ProductDetail { get; set; }
        [NotMapped]
        public decimal? LeadTime { get; set; }
        [NotMapped]
        public ICollection<AccsExt>? AccsExts { get; set; }
        public string? PromoType { get; set; }
        public long? PromosID { get; set; }
        public bool? FlagPromo { get; set; }
        public int? AccFlag { get; set; }
        [NotMapped]
        public Sopra.Entities.Packaging? Packagings { get; set; }
    }

    public class AccsExt
    {
        [NotMapped]
        public long? RefID { get; set; }

        [NotMapped]
        public long? CartsID { get; set; }

        [NotMapped]
        public long? CartDetailsID { get; set; }

        [NotMapped]
        public long? ObjectID { get; set; }

        [NotMapped]
        public long? ParentID { get; set; }

        [NotMapped]
        public string? Type { get; set; }

        [NotMapped]
        public decimal? QtyBox { get; set; }

        [NotMapped]
        public decimal? Qty { get; set; }

        [NotMapped]
        public decimal? Price { get; set; }

        [NotMapped]
        public decimal? Amount { get; set; }

        [NotMapped]
        public bool? IsCheckout { get; set; }
        [NotMapped]
        public Product? ProductDetail { get; set; }
        [NotMapped]
        public string? Notes { get; set; }
        [NotMapped]
        public decimal? LeadTime { get; set; }
    }
}
