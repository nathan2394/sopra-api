using System.Text.Json.Serialization;

namespace Sopra.Entities
{
    public class TokenResponse
    {
        public string access_token { get; set; }
    }
    
    public class ZohoLoginRequest
	{
		public string Code { get; set; }
		public string RedirectUri { get; set; }
	}

    public class ZohoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }

    public class ZohoUserInfo
    {
        public string Email { get; set; }
        
        [JsonPropertyName("First_Name")]
        public string FirstName { get; set; }
        
        [JsonPropertyName("Last_Name")]
        public string LastName { get; set; }
        
        [JsonPropertyName("ZUID")]
        public string UserId { get; set; }
        
        [JsonPropertyName("profile_picture")]
        public string ProfilePicture { get; set; }
    }
}
