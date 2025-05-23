using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Shapes")]
    public class Shape : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
