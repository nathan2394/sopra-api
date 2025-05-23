using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "Promos")]
    public class Promo : Entity
    {
        public long? RefID { get; set; }
        public string Name { get; set; }
        public string PromoDesc { get; set; }
        public string Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Image { get; set; }
        public string ImgThumb { get; set; }
        public int? Category { get; set; }
        [NotMapped]
        public PromoProduct? PromoProduct { get; set; }
        [NotMapped]
        public PromoQuantity? PromoQuantity { get; set; }
        [NotMapped]
        public PromoMixDetail? PromoMix { get; set; }
    }
}
