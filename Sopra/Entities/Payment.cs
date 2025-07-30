using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Payments")]
    public class Payment : Entity
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public string? PaymentNo { get; set; }
        public string? Type { get; set; }
        public DateTime? TransDate { get; set; }
        public long? CustomersID { get; set; }
        public long? InvoicesID { get; set; }
        public decimal? Netto { get; set; }
        public string? BankRef { get; set; }
        public DateTime? BankTime { get; set; }
        public decimal? AmtReceive { get; set; }
        public string? Status { get; set; }
        public long? ReasonsID { get; set; }
        public string? Note { get; set; }
        public string? Username { get; set; }
        public string? UsernameCancel { get; set; }
        public long? CompaniesID { get; set; }
        public string? ExternalNo { get; set; }
    }
}
