using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Colors")]
    public class Color : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
