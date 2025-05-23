using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Regencies")]
    public class Regency : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public long ProvincesID { get; set; }
    }
}
