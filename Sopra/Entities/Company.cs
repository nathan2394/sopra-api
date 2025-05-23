using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Companies")]
    public class Company : Entity
    {
        public long RefID { get; set; }
        public string? Name { get; set; }
        public string? PtName { get; set; }
    }
}
