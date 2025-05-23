using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "AltNecks")]
    public class AltNeck : Entity
    {
        public long RefID { get; set; }
        public string Grouping { get; set; }
    }
}
