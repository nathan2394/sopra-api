using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Customers")]
    public class Customer : Entity
    {
        public long RefID { get; set; }
        public string? Name { get; set; }
        public string? CustomerNumber { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? BillingAddress { get; set; }
        public long? DeliveryRegencyID { get; set; }
        public long? DeliveryProvinceID { get; set; }
        public long? CountriesID { get; set; }
        public long? DeliveryPostalCode { get; set; }
        public string? Mobile1 { get; set; }
        public string? PIC { get; set; }
        public string? Email { get; set; }
        public string? Termin { get; set; }
        public string? Currency { get; set; }
        public string? Tax1 { get; set; }
        public string? NPWP { get; set; }
        public string? NIK { get; set; }
        public string? TaxType { get; set; }
        public string? VirtualAccount { get; set; }
        public string? Seller { get; set; }
        public long? CustomerType { get; set; }
        public string? Mobile2 { get; set; }
        public long? DeliveryDistrictID { get; set; }
        public string? DeliveryPhone { get; set; }
        public long? Status { get; set; }
        public long? SalesID { get; set; }
        public string? OtpCode { get; set; }
        public DateTime? OtpDatetime { get; set; }
    }
}
