using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "TransactionOrderDetails")]
    public class TransactionOrderDetail : Entity
    {
        public long RefID { get; set; }
        public long CompanyID { get; set; }
        public long OrdersID { get; set; }
        public string? OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public long CustomersID { get; set; }
        public string? ProvinceName { get; set; }
        public string? RegencyName { get; set; }
        public string? DistrictName { get; set; }
        public long ObjectID { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
        public long Type { get; set; }
        public long? InvoicesID { get; set; }
        public string? InvoiceNo { get; set; }
        public long? PaymentsID { get; set; }
        public string? PaymentsNo { get; set; }
        public string Status { get; set; }
        public long LinkedWMS { get; set; }
        public string DealerTier { get; set; }
        public string? OrderStatus { get; set; }
    }
}
