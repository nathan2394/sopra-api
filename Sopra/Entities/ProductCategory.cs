using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "ProductCategories")]
    public class ProductCategory : Entity
    {
        public long RefID { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }

		[NotMapped]
		public string? RealImage { get; set; }

        public string? Keyword { get; set; }
        public string? Type { get; set; }
        public long? Seq { get; set; }
    }
}
