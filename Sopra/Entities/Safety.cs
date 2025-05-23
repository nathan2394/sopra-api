using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Safeties")]
    public class Safety : Entity
    {
        public long RefID { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public string? Image { get; set; }

        public string? Note { get; set; }
    }
}
