using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "TagVideos")]
    public class TagVideo : Entity
    {
        public long? RefID { get; set; }
        public long? TagsID { get; set; }
        public string? VideoLink { get; set; }
        public string? Description { get; set; }
    }
}
