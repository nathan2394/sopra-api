using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "SearchKeywords")]
    public class SearchKeyword : Entity
    {
        public long RefID { get; set; }
        public string? Keyword { get; set; }
        public string? CorrectKeyword { get; set; }
    }
}
