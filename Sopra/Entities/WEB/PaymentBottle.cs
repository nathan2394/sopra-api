using System;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class PaymentBottle
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public long InvoicesID { get; set; }
        public long? PaymentMethod { get; set; }
        public string? PaymentNo { get; set; }
        public string? Type { get; set; }
        public string? BankRef { get; set; }
        public string? Status { get; set; }
        public decimal? Netto { get; set; }
        public decimal? AmtReceive { get; set; }
        public long? CustomersID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? TransDate { get; set; }
        public DateTime? BankTime { get; set; }
        public long? CompanyID { get; set; }
        public long? userId { get; set; }
    }
}