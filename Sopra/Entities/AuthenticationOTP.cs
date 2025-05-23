using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    public class AuthenticationOTPRequest
    {
        public string Phone { get; set; }
		public bool Success { get; set; }
		public string? Message { get; set; }
	}
}
