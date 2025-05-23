using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "WishLists")]
    public class WishList : Entity
    {
        public long? RefID { get; set; }
        public int? UserId { get; set; }
        public int? ProductId { get; set; }
        public string? Type { get; set; }
        [NotMapped]
        public Product? ProductDetail { get; set; }
    }
}
