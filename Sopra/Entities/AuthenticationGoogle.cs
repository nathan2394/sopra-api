using System.ComponentModel.DataAnnotations;

namespace Sopra.Entities
{
    public class GoogleLoginRequest
    {
        [Required]
        public string GoogleToken { get; set; }

        public string IpAddress { get; set; }
    }

    public class GoogleUserInfo
    {
        public string GoogleId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }
}
