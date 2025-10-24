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
        public bool? IsDP { get; set; }
        public long? userId { get; set; }
    }

    public class VATransaction
    {
        public long company_id { get; set; }
        public string? first_name { get; set; }
        public long id { get; set; }
        public DateTime? updated_at { get; set; }
        public decimal netto { get; set; }
        public decimal bill { get; set; }
        public string? cust_num { get; set; }
        public string va_num { get; set; }
        public long? payment_id { get; set; }
        public string type { get; set; }
        public long isNew { get; set; }
    }

    public class InvoiceDetail
    {
        public Invoice? Invoice { get; set; }
        public Payment? Payment { get; set; }
        public string? CustomerName { get; set; }
    }
}