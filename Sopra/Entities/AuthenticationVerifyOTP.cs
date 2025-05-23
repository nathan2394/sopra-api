using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    public class AuthenticationVerifyOTPRequest
    {
        public string Code { get; set; }
    }
}
