using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Credentials")]
    public class Credential : Entity
    {
        //public long id { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string? ExternalId { get; set; }
        public string? RequestId { get; set; }
        public string? FlagStatus { get; set; }
        public string? FlagReason { get; set; }
    }
}
