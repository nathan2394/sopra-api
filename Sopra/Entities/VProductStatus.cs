using Google.Cloud.Vision.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    //[Keyless]
    public class VProductStatus
    {
        public long? ProductId { get; set; }
        public string? WmsCode { get; set; }
        public string? ProductName { get; set; }
        public decimal? TotalQty { get; set; }
        public decimal? ShippingQty { get; set; }
        public decimal? Outstanding { get; set; }
        public decimal? DataStock { get; set; }
        public decimal? StockAvail { get; set; }
        public string? StockStatus { get; set; }
        public int? CountOrder { get; set; }
        public int? Wishlist { get; set; }
        public decimal? TotalShared { get; set; }
        public decimal? TotalViews { get; set; }
        public decimal? Score { get; set; }
        public int? PrepTime { get; set; }
        public decimal? ProdTime { get; set; }
        public decimal? LeadTime { get; set; }
        public decimal? DailyOutput { get; set; }
    }
}
