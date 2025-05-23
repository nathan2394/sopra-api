using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Countries")]
    public class Country : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
    }
}
