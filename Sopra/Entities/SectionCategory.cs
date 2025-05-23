using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "SectionCategories")]
    public class SectionCategory : Entity
    {
        public long RefID { get; set; }
        public string? SectionTitle { get; set; }
        public string? SectionTitleEN { get; set; }
        public long? Seq { get; set; }
        public string? Status { get; set; }
        public string? ImgBannerDesktop { get; set; }
        public string? ImgBannerMobile { get; set; }
        public string? Description { get; set; }

		[NotMapped]
		public List<ProductCategory>? CategoryList { get; set; }
        [NotMapped]
        public List<Product>? Product { get; set; }
    }
}
