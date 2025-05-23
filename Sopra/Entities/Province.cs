using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Provinces")]
    public class Province : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public long CountriesID { get; set; }
    }
}
