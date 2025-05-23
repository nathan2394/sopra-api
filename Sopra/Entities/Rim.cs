using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Rims")]
    public class Rim : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
    }
}
