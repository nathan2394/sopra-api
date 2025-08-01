﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "OrderDetails")]
    public class OrderDetail : Entity
    {
        public long? RefID { get; set; }
        public long? OrdersID { get; set; }
        public long? ObjectID { get; set; }
        public long? ParentID { get; set; }
        public string? ObjectType { get; set; }
        public long? PromosID { get; set; }
        public string? Type { get; set; }
        public decimal? QtyBox { get; set; }
        public decimal? Qty { get; set; }
        public decimal? ProductPrice { get; set; }
        public decimal? Amount { get; set; }
        public bool? FlagPromo { get; set; }
        public decimal? Outstanding { get; set; }
        public string? Note { get; set; }
        public long? CompaniesID { get; set; }
        public Order Orders { get; set; }
        [NotMapped]
        public ICollection<AccsExtOrder>? AccsExtOrders { get; set; }
        [NotMapped]
        public ProductDetail2? ProductDetail { get; set; }
        [NotMapped]
        public decimal? LeadTime { get; set; }
        public string? PromosType { get; set; }
        public int? AccFlag { get; set; }
        public string? ApprovalStatus { get; set; }
    }

    public class AccsExtOrder
    {
        [NotMapped]
        public long? RefID { get; set; }

        [NotMapped]
        public long? OrdersID { get; set; }

        [NotMapped]
        public long? OrderDetailsID { get; set; }

        [NotMapped]
        public long? ObjectID { get; set; }

        [NotMapped]
        public long? ParentID { get; set; }

        [NotMapped]
        public string Type { get; set; }

        [NotMapped]
        public decimal? QtyBox { get; set; }

        [NotMapped]
        public decimal? Qty { get; set; }

        [NotMapped]
        public decimal? Price { get; set; }

        [NotMapped]
        public decimal? Amount { get; set; }
        [NotMapped]
        public Product? ProductDetail { get; set; }
        [NotMapped]
        public string? Note { get; set; }
        [NotMapped]
        public decimal? LeadTime { get; set; }
    }
}
