using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Favorites")]
    public class Favorite : Entity
    {
        public long RefID { get; set; }
        public long? ObjectID { get; set; }
        public string? Type { get; set; }
        public long? Accs1ID { get; set; }
        public long? Accs2ID { get; set; }
        public decimal? SalesPrice { get; set; }
        public decimal? PriceAcc1 { get; set; }
        public decimal? PriceAcc2 { get; set; }
        public long? UsersID { get; set; }
    }
}
