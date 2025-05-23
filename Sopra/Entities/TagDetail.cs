using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "TagDetails")]
    public class TagDetail : Entity
    {
        public long RefID { get; set; }
        public long TagsID { get; set; }
        public long ObjectID { get; set; }
        public string Type { get; set; }
    }
}
