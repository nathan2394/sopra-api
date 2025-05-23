using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "ProductStatus")]
    public class ProductStatus 
    {
        public long? ProductID { get; set; }
        public string? WmsCode { get; set; }
        public string? ProductName { get; set; }
        public decimal? TotalQty { get; set; }
        public decimal? ShippingQty { get; set; }
        public decimal? Outstanding { get; set; }
        public int? DataStock { get; set; }
        public decimal? DailyOutput { get; set; }
        public decimal? StockAvail { get; set; }
        public string? StockStatus { get; set; }
        public long? CountOrder { get; set; }
        public long? WishList { get; set; }
        public int? TotalShared { get; set; }
        public int? TotalViews { get; set; }
        public long? Score { get; set; }
        public long? PrepTime { get; set; }
        public decimal? ProdTime { get; set; }
        public decimal? LeadTime { get; set; }
    }
}
