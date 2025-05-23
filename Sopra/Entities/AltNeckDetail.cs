using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "AltNeckDetails")]
    public class AltNeckDetail : Entity
    {
        public long RefID { get; set; }
        public long AltNecksID { get; set; }
        public long CalculatorsID { get; set; }
    }
}
