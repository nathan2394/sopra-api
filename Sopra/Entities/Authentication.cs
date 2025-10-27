using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sopra.Entities
{
    public class AuthenticationRequest
    {
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }
        public bool IsRestricted { get; set; }
    }
}
