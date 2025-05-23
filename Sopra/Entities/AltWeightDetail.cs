using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "AltWeightDetails")]
    public class AltWeightDetail : Entity
    {
        public long RefID { get; set; }
        public long AltWeightsID { get; set; }
        public long ObjectID { get; set; }
        public string Type { get; set; }
    }
}
