using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Necks")]
    public class Neck : Entity
    {
        public long RefID { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
    }
}
