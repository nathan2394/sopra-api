using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Tags")]
    public class Tag : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
    }
}
