using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Packagings")]
    public class Packaging : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Thickness { get; set; }
        public decimal Height { get; set; }
        public int Tipe { get; set; }
    }
}
