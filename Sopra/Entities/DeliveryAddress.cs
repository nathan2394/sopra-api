using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "DeliveryAddresses")]
    public class DeliveryAddress : Entity
    {
        public long RefID { get; set; }
        public long? DistrictId { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string? Label { get; set; }
        public string? Landmark { get; set; }
        public long CityId { get; set; }
        public long? ProvinceId { get; set; }
        public long? CountryId { get; set; }
        public long? UserId { get; set; }
        public bool? IsUse { get; set; }
    }
}
