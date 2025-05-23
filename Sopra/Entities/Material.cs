using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Materials")]
    public class Material : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public string PlasticType { get; set; }
        public long? Halal { get; set; }
        public long? FoodGrade { get; set; }
        public long? BpaFree { get; set; }
        public long? EcoFriendly { get; set; }
        public long? Recyclable { get; set; }
    }
}
