using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "AltVolumes")]
    public class AltVolume : Entity
    {
        public long RefID { get; set; }
        public string Grouping { get; set; }
    }
}
