using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "AltVolumeDetails")]
    public class AltVolumeDetail : Entity
    {
        public long RefID { get; set; }
        public long AltVolumesID { get; set; }
        public long CalculatorsID { get; set; }
    }
}
