using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Districts")]
    public class District : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public long RegenciesID { get; set; }
    }
}
