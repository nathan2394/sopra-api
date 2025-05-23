using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "PopularityIndicators")]
    public class PopularityIndicator : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
        public string Description { get; set; }
        public int PrepTime { get; set; }
    }
}
