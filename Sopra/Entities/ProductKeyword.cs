using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "ProductKeywords")]
    public class ProductKeyword
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? NameEn { get; set; }
    }
}
