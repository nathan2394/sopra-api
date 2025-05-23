using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Images")]
    public class Image : Entity
    {
        public long RefID { get; set; }
        public string ProductImage { get; set; }
        public string Type { get; set; }
        public long ObjectID { get; set; }
    }
}
