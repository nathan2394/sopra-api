using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Reasons")]
    public class Reason : Entity
    {
        public long RefID { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? NameEN { get; set; }
    }
}
