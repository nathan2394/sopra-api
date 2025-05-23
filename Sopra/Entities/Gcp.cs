using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Gcps")]
    public class Gcp : Entity
    {
        public long? RefID { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }

        [NotMapped]
        public string? PublicUrl { get; set; }
        [NotMapped]
        public string? AuthenticatedUrl { get; set; }
    }
}
