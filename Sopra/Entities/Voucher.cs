using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Vouchers")]
    public class Voucher : Entity
    {
        public long RefID { get; set; }
        public string? VoucherNo { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public long? OrdersID { get; set; }
        public long? Disc { get; set; }
        public long? VoucherUsage { get; set; }
        public decimal? MinOrder { get; set; }
        public string? Status { get; set; }
        public string? FlagOrder { get; set; }
    }
}
