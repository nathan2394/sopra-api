using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "AltWeights")]
    public class AltWeight : Entity
    {
        public long RefID { get; set; }
        public string Grouping { get; set; }
        public string Type { get; set; }
    }
}
