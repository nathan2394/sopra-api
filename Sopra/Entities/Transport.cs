using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Transports")]
    public class Transport : Entity
    {
        public long RefID { get; set; }
        public string? Name { get; set; }
        public decimal? Cost { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public long? Type { get; set; }
    }
}
