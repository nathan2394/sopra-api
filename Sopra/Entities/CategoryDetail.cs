using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "CategoryDetails")]
    public class CategoryDetail : Entity
    {
        public long RefID { get; set; }
        public long CategoriesID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Color { get; set; }
        public string Type { get; set; }
    }
}
