using System;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class InvoiceBottle
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public long OrdersID { get; set; }
        public long? PaymentMethod { get; set; }
        public string? InvoiceNo { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public decimal? Netto { get; set; }
        public long? CustomersID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? TransDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? VANum { get; set; }
        public string? CustNum { get; set; }
        public decimal? Refund { get; set; }
        public decimal? Bill { get; set; }
        public long? CompanyID { get; set; }
        public long? SentWaCounter { get; set; }
        public DateTime? WASentTime { get; set; }
        public long? FlagInv { get; set; }
        public long? userId { get; set; }
    }
}