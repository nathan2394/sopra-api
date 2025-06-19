using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Users")]
    public class User : Entity
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
		public string? Name { get; set; }
		public long? RoleID { get; set; }
		public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public long? CustomersID { get; set; }
        public string? PublicationName { get; set; }
        public string? PublicationPIC { get; set; }
        public long? PublicationProvincesID { get; set; }
        public long? PublicationDistrictsID { get; set; }
        public long? PublicationRegenciesID { get; set; }
        public string? PublicationPhone1 { get; set; }
        public string? PublicationPhone2 { get; set; }
        public string? CustNum { get; set; }
        public long? CustomerGroup { get; set; }
        public DateTime? LastLoginDates { get; set; }
        public long? CompanyID { get; set; }
        public string? Subdomain { get; set; }
        public string? FirebaseToken { get; set; }
        [NotMapped]
        public Dealer? Dealer { get; set; }
        [NotMapped]
        public Customer? Customer { get; set; }

        // public Role Role { get; set; }
    }
}
