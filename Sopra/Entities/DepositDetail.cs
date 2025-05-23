using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "DepositDetails")]
    public class DepositDetail : Entity
    {
        public long? RefID { get; set; }
        public long? DepositsID { get; set; }
        public long? FromPaymentsID { get; set; }
        public string? FromPaymentType { get; set; }
        public long? ToPaymentsID { get; set; }
        public string? ToPaymentType { get; set; }
        public string? PIC { get; set; }
        public bool? IsReturn { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal? Amount { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public DateTime? TransferDate { get; set; }
        public decimal? TransferAmount { get; set; }

    }
}
