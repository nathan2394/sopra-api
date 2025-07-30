using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Invoices")]
    public class Invoice : Entity
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public long? OrdersID { get; set; }
        public long? PaymentMethod { get; set; }
        public string? InvoiceNo { get; set; }
        public string? Type { get; set; }
        public decimal? Netto { get; set; }
        public long? CustomersID { get; set; }
        public DateTime? TransDate { get; set; }
        public string? Status { get; set; }
        public string? VANum { get; set; }
        public string? CustNum { get; set; }
        public long? FlagInv { get; set; }
        public long? ReasonsID { get; set; }
        public string? Note { get; set; }
        public decimal? Refund { get; set; }
        public decimal? Bill { get; set; }
        public long? PICInv { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public DateTime? TransferDate { get; set; }
        public string? BankRef { get; set; }
        public decimal? TransferAmount { get; set; }
        public string? Username { get; set; }
        public string? UsernameCancel { get; set; }
        public long? XenditID { get; set; }
        public string? XenditBank { get; set; }
        public string? PDFFile { get; set; }
        public DateTime? DueDate { get; set; }
        public long? CompaniesID { get; set; }
        public long? SentWaCounter { get; set; }
        public DateTime? WASentTime { get; set; }
        public bool? FutureDateStatus { get; set; }
        public bool? CreditStatus { get; set; }
        public string? ExternalNo { get; set; }

    }
}
