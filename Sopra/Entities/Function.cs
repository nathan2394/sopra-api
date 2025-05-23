using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Functions")]
    public class Function : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public string NameEN { get; set; }
    }
}
