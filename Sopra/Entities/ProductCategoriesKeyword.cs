using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "ProductCategoriesKeywords")]
    public class ProductCategoriesKeyword
    {
        public long ID { get; set; }
        public long? ProductCategoriesID { get; set; }
        public long? ProductKeywordID { get; set; }
    }
}
