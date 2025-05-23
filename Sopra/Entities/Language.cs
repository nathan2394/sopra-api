using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Languages")]
    public class Language : Entity
    {
        public long RefID { get; set; }
        public string Content { get; set; }
        public string NameID { get; set; }
        public string NameEN { get; set; }
    }
}
