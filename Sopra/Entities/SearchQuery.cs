using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "SearchQueries")]
    public class SearchQuery : Entity
    {
        public long RefID { get; set; }
        public string? Keyword { get; set; }
        public long? SearchFrequency { get; set; }
    }
}
