using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Banners")]
    public class Banner : Entity
    {
        public long? RefID { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public string? nameInd { get; set; }
        public string? nameEng { get; set; }
        [NotMapped]
        public List<BannerCategory>? CategoryList { get; set; }
    }

    public class BannerCategory 
    {
        [NotMapped]
        public long? BannerID { get; set; }
        [NotMapped]
        public List<string>? nameInd { get; set; }
        [NotMapped]
        public List<string>? nameEng { get; set; }
    }
}
