using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    //[Keyless]
    public class VTransactionOrderDetail
    {
        public long? OrderCompany{ get; set; }
        public long? OrderID { get; set; }
        public string OrderNo { get; set; }
        public DateTime? OrderDate { get; set; }
        public long? CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string ProvinceName { get; set; }
        public string DistrictName { get; set; }
        public string RegenciesName { get; set; }
        public long? ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Amount { get; set; }
        public long? InvoiceID { get; set; }
        public string InvoiceNo { get; set; }
        public long? PaymentID { get; set; }
        public string PaymentNo { get; set; }
        public string Status { get; set; }
        public int? LinkedWms { get; set; }
        public string DealerTier { get; set; }
        public string OrderStatus { get; set; }
        public decimal? Delivered { get; set; }
        public decimal? TotalQty { get; set; }
    }
}
