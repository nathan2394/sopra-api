using Google.Apis.Auth;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Sopra.Responses;
using Sopra.Helpers;
using Sopra.Entities;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore;

namespace Sopra.Services
{
	public class AuthService : IAuthService
	{
		private readonly EFContext context;
		private readonly IMemoryCache memoryCache;
		private readonly IConfiguration config;
		// private readonly IUserLogService _userLog;

		public AuthService(EFContext context, IMemoryCache memoryCache, IConfiguration config)
		{
			this.context = context;
			this.memoryCache = memoryCache;
			this.config = config;
			// _userLog = userLog;
		}

		public User Authenticate(string email, string password, string ipAddress)
		{
			var user = context.Users.FirstOrDefault(x => x.Email == email && x.IsDeleted == false);

			try
			{
				if (user == null)
					return null;

				if (!Helpers.Utility.VerifyHashedPassword(user.Password, password))
				{
					// await _userLog.UserLog(user.ID, $"{email} login with IP Address {ipAddress} - Failed");
					return null;
				}

                // await _userLog.UserLog(user.ID, $"{email} login with IP Address {ipAddress}");
                // user.Password = string.Empty;

                //var token = GenerateToken(user);
                //var response = new AuthResponse(user, token);

                //return response;
                DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
                TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
                DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7

                user.Password = "";	

                var userDealer = this.context.UserDealers.Where(x => x.UserId == user.RefID && gmtPlus7Time < x.EndDate).FirstOrDefault();
				if(userDealer != null) user.Dealer = this.context.Dealers.FirstOrDefault(x => x.RefID == userDealer.DealerId);

                var customer = this.context.Customers.FirstOrDefault(x => x.RefID == user.CustomersID);
				if (customer != null) user.Customer = customer;

                return user;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				return null;
			}
			finally
			{
				context.Dispose();
			}
		}

        public async Task<AuthResponse> UserAuthenticate(string email,  string firebaseToken)
        {

            var saveToken = await this.context.Users
                .Where(x => x.Email == email && x.IsDeleted != true)
                .FirstOrDefaultAsync();

            saveToken.FirebaseToken = firebaseToken;

            await this.context.SaveChangesAsync();

			var user = await this.context.Users.FirstOrDefaultAsync(x => x.Email == email);

			if (user == null)
                return null;

            user.Password = string.Empty;

            var token = GenerateToken(user);

            var response = new AuthResponse(user, token);

            return response;
        }

        public async Task<AuthenticationOTPRequest> AuthenticateOTP(string phone, string ipAddress)
		{
			if (phone.StartsWith("62")) phone = phone.Replace("62", "0");
			else if (phone.StartsWith("8")) phone = "0" + phone;
            var query = from a in context.Users
						join b in context.Customers
						on a.CustomersID equals b.RefID 
						where b.Mobile1 == phone 
						&& a.IsDeleted == false
						&& b.IsDeleted == false
						&& b.Status == 1
						select a;

			var user = query.FirstOrDefault();
			try
			{
				if (user == null)
					return new AuthenticationOTPRequest { Success = false, Message = "User not found." };

				//generate OTP 
				string otp = GenerateOTP();
				var customer = context.Customers.FirstOrDefault(c => c.Mobile1 == phone && c.RefID == user.CustomersID);
				if (customer != null)
				{
					customer.OtpCode = otp;
					customer.OtpDatetime = DateTime.Now;
					context.SaveChanges();
				}

				string userName = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName : user.Name;
				// Send OTP via WhatsApp
				await SendOTP(userName, phone, otp);

				return new AuthenticationOTPRequest { Success = true, Message = "OTP sent successfully." };

			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				return new AuthenticationOTPRequest { Success = false, Message = "Error occurred while processing the request." };
			}
			finally
			{
				context.Dispose();
			}
		}
		
		public User AuthenticateVerifyOTP(string code, string ipAddress)
		{
			var query = from a in context.Users
						join b in context.Customers
						on a.CustomersID equals b.RefID
						where b.OtpCode == code
						&& a.IsDeleted == false
						&& b.IsDeleted == false
						&& b.Status == 1
						select b;

			var customer = query.FirstOrDefault();
			try
			{
				if (customer == null)
					return null;

				if (customer.OtpDatetime.HasValue && (DateTime.Now - customer.OtpDatetime.Value).TotalMinutes > 2)
				{
					// If OTP was sent more than 2 minutes ago, return null
					return null;
				}

				// Fetch the user associated with the customer
				var user = context.Users.FirstOrDefault(u => u.CustomersID == customer.RefID);

				if (user == null)
					return null;

				// Clear password for security reasons
				user.Password = "";

				return user;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				return null;
			}
			finally
			{
				context.Dispose();
			}
		}

		public string GenerateToken(User user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var secret = config.GetSection("AppSettings")["Secret"];
			var key = Encoding.ASCII.GetBytes(secret);
			var claims = new ClaimsIdentity(new[]
			{
				new Claim("id", user.ID.ToString()),
				//new Claim("name", user.Name),
				new Claim("roleid", user.RoleID.ToString())
			  });

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = claims,
				Expires = DateTime.UtcNow.AddDays(7),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		public async Task<User> AuthenticateWithGoogle(string googleToken, string ipAddress)
		{
			try
			{
				var googleUser = await VerifyGoogleToken(googleToken);
				if (googleUser == null)
				{
					return null;
				}

				var user = context.Users.FirstOrDefault(x => x.Email == googleUser.Email && x.IsDeleted == false);
				
				if (user == null)
				{
					return null;
				}

				DateTime utcNow = DateTime.UtcNow;
				TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
				DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7);

				user.Password = "";

				var userDealer = this.context.UserDealers.Where(x => x.UserId == user.RefID && gmtPlus7Time < x.EndDate).FirstOrDefault();
				if (userDealer != null) user.Dealer = this.context.Dealers.FirstOrDefault(x => x.RefID == userDealer.DealerId);

				var customer = this.context.Customers.FirstOrDefault(x => x.RefID == user.CustomersID);
				if (customer != null) user.Customer = customer;

				return user;
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Google authentication error: {ex.Message}");
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);
				return null;
			}
			finally
			{
				context.Dispose();
			}
		}

		private async Task<GoogleUserInfo> VerifyGoogleToken(string token)
		{
			try
			{
				var googleClientId = config.GetSection("GoogleAuth")["ClientId"];
				
				var payload = await GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings()
				{
					Audience = new[] { googleClientId }
				});

				return new GoogleUserInfo
				{
					GoogleId = payload.Subject,
					Email = payload.Email,
					Name = payload.Name,
				};
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Google token verification failed: {ex.Message}");
				return null;
			}
		}

		private string GenerateOTP()
		{
			// Generate a random 4-digit OTP
			Random random = new Random();
			int otp = random.Next(1000, 9999);
			return otp.ToString();
		}

		private string FormatPhoneNumber(string number)
		{
			//if (number.StartsWith("0")) return number;
			//         else if (number.StartsWith("62")) return number;
			//         else if (!number.StartsWith("0") && !number.StartsWith("62") && number.StartsWith("8")) return number;

			number = number.TrimStart('0');

			if (!number.StartsWith("62"))
			{
				number = "62" + number;
			}

			return number;
		}

		private async Task SendOTP(string name, string number, string otp)
		{
			try
			{
				// Format phone number
				var formattedNumber = FormatPhoneNumber(number);

				// Prepare authentication data
				var username = "dgtmkt@solusi-pack.com";
				var password = "Admin123!";
				var grantType = "password";
				var clientId = "RRrn6uIxalR_QaHFlcKOqbjHMG63elEdPTair9B9YdY";
				var clientSecret = "Sa8IGIh_HpVK1ZLAF0iFf7jU760osaUNV659pBIZR00";
				var tokenUrl = "https://service-chat.qontak.com/api/open/v1/oauth/token";

				// Prepare HttpClient instance
				using (var client = new HttpClient())
				{
					client.BaseAddress = new Uri(tokenUrl);

					// Prepare request body
					var requestBody = new
					{
						username,
						password,
						grant_type = grantType,
						client_id = clientId,
						client_secret = clientSecret
					};

					// Log request body
					// Console.WriteLine($"Token Request Body: {JsonSerializer.Serialize(requestBody)}");

					// Convert request body to JSON
					var jsonRequest = JsonSerializer.Serialize(requestBody);

					// Prepare request content
					var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

					// Send token request
					var tokenResponse = await client.PostAsync(tokenUrl, content);

					// Log request URL
					// Console.WriteLine($"Token Request URL: {tokenUrl}");

					// Log response status code
					// Console.WriteLine($"Token Response Status Code: {tokenResponse.StatusCode}");

					// Check if token request was successful
					if (tokenResponse.IsSuccessStatusCode)
					{
						// Read token response
						var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
						var token = JsonSerializer.Deserialize<TokenResponse>(tokenContent);

                        // Prepare OTP message data
                        var templateId = "7bc159f1-349d-4ede-859c-a4b61ed6cb73";
						var channelId = "1a354b7e-d46b-470b-a8e7-9e5841e48b1b";
						var sendMessageUrl = "https://service-chat.qontak.com/api/open/v1/broadcasts/whatsapp/direct";

						var messageBody = new
						{
							to_name = name,
							to_number = formattedNumber,
							message_template_id = templateId,
							channel_integration_id = channelId,
							language = new { code = "en" },
							parameters = new
							{
								body = new[]
								{
									new { key = "1", value_text = otp, value = "10" }
								},
								buttons = new[]
                                {
                                    new { index = "0", type = "URL", value = otp }
                                }
                            }
						};

						// Log message body
						// Console.WriteLine($"Message Body: {JsonSerializer.Serialize(messageBody)}");

						// Convert message body to JSON
						var jsonMessageBody = JsonSerializer.Serialize(messageBody);

						// Prepare request content for sending message
						var sendMessageContent = new StringContent(jsonMessageBody, Encoding.UTF8, "application/json");

						// Log request body
						// Console.WriteLine($"Send Message Request Body: {jsonMessageBody}");

						var requestMessage = new HttpRequestMessage(HttpMethod.Post, sendMessageUrl);
						requestMessage.Content = sendMessageContent;
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

						// Log request URL
						// Console.WriteLine($"Send Message Request URL: {sendMessageUrl}");

						// Send OTP message
						var sendMessageResponse = await client.SendAsync(requestMessage);

						// Log response status code
						// Console.WriteLine($"Send Message Response Status Code: {sendMessageResponse.StatusCode}");

						// Check if OTP message was sent successfully
						if (sendMessageResponse.IsSuccessStatusCode)
						{
							Console.WriteLine($"{name} Send OTP code to {number}");
							// Log success or handle as needed
						}
						else
						{
							Console.WriteLine($"{name} Failed Send OTP code to {number}. StatusCode: {sendMessageResponse.StatusCode}");
							// Log failure or handle as needed
						}
					}
					else
					{
						Console.WriteLine("Failed to retrieve access token.");
						// Log failure or handle as needed
					}
				}
					
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred while sending OTP: {ex.Message}");
				// Log any exceptions
			}
		}

		// Define a class to represent the token response
		public class TokenResponse
		{
			public string access_token { get; set; }
		}

	}
}