using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Categories")]
    public class Category : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        
        [NotMapped]
        public List<CategoryDetail>? Details { get; set; }
    }
}
