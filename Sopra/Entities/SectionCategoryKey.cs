using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "SectionCategoryKeys")]
    public class SectionCategoryKey : Entity
    {
        public long RefID { get; set; }
        public long? SectionCategoriesID { get; set; }
        public long? PoductCategoriesID { get; set; }
        public string? ImgTheme { get; set; }
        public string? Description { get; set; }
    }
}
