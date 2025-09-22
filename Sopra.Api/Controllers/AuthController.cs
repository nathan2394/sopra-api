using Microsoft.AspNetCore.Mvc;
using Sopra.Entities;
using Sopra.Responses;
using Sopra.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
		private IAuthService _service;
        private IServiceAsync<User> _userService;

        public AuthController(IAuthService service, IServiceAsync<User> userService)
		{
			_service = service;
			_userService = userService;

        }

		[HttpPost("login")]
		public IActionResult Authenticate([FromQuery] AuthenticationRequest request)
		{
			try
			{
				var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

				var user = _service.Authenticate(request.Email, request.Password, ipAddress);
				if (user == null)
					return BadRequest(new { message = "Email or password is incorrect" });

				var token = _service.GenerateToken(user);
				var response = new AuthResponse(user, token);

				return Ok(response);
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				var inner = ex.InnerException;
				while (inner != null)
				{
					message = inner.Message;
					inner = inner.InnerException;
				}
				Trace.WriteLine(message, "AuthController");
				return BadRequest(new { message });
			}
		}

		[HttpPost("google-login")]
		public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
		{
			try
			{
				var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

				var user = await _service.AuthenticateWithGoogle(request.GoogleToken, ipAddress);
				if (user == null)
				{
					return BadRequest(new { message = "User can't be found" });
				}

				var token = _service.GenerateToken(user);
				
				return Ok(new 
				{ 
					data = user, 
					token = token,
				});
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Google login endpoint error: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error during Google authentication" });
			}
		}

		[HttpPost("zoho-login")]
		public async Task<IActionResult> ZohoLogin([FromBody] ZohoLoginRequest request)
		{
			try
			{
				if (string.IsNullOrEmpty(request.Code))
				{
					return BadRequest(new { message = "Authorization code is required" });
				}

				if (string.IsNullOrEmpty(request.RedirectUri))
				{
					return BadRequest(new { message = "Redirect URI is required" });
				}

				var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

				var user = await _service.AuthenticateWithZoho(request.Code, request.RedirectUri, ipAddress);
				if (user == null)
				{
					return BadRequest(new { message = "User can't be found" });
				}

				var token = _service.GenerateToken(user);
				
				return Ok(new 
				{ 
					data = user, 
					token = token,
				});
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Zoho login endpoint error: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error during Zoho authentication" });
			}
		}

		[HttpPost("login/otp")]
		public async Task<IActionResult> AuthenticateOTP([FromQuery(Name = "phone")] string phone)
		{
			try
			{
				if (string.IsNullOrEmpty(phone))
					return BadRequest(new { message = "Phone number cannot be empty" });

				var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

				var result = await _service.AuthenticateOTP(phone, ipAddress);
				if (!result.Success)
					return BadRequest(new { message = result.Message });

				//var token = _service.GenerateToken(user);
				//var response = new AuthResponse(user, token);

				return Ok(new { message = result.Message });
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				var inner = ex.InnerException;
				while (inner != null)
				{
					message = inner.Message;
					inner = inner.InnerException;
				}
				Trace.WriteLine(message, "AuthController");
				return BadRequest(new { message });
			}
		}

        //[Authorize]
        [HttpGet("current-user")]
        public async Task<IActionResult> CurrentUser(string firebaseToken="",long userId=0)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var uId = userId;
                var token = "";
                var user = await _userService.GetByIdAsync(uId);
                if (user != null)
                {
                   var userResponse = await _service.UserAuthenticate(user.Email,  firebaseToken);
                   token = userResponse.Token;
                   user.FirebaseToken = firebaseToken;
                }
				user.Password = "";
				var response = new AuthResponse(user, token);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "AuthController");
                return BadRequest(new
                {
                    message
                });
            }
        }

        [HttpPost("login/verify/otp")]
		public IActionResult AuthenticateVerifyOTP([FromQuery] AuthenticationVerifyOTPRequest request)
		{
			try
			{
				if (string.IsNullOrEmpty(request.Code))
					return BadRequest(new { message = "Code cannot be empty" });

				var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

				var user = _service.AuthenticateVerifyOTP(request.Code, ipAddress);
				if (user == null)
					return BadRequest(new { message = "Code OTP is incorrect" });

				var token = _service.GenerateToken(user);
				var response = new AuthResponse(user, token);

				return Ok(response);
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				var inner = ex.InnerException;
				while (inner != null)
				{
					message = inner.Message;
					inner = inner.InnerException;
				}
				Trace.WriteLine(message, "AuthController");
				return BadRequest(new { message });
			}
		}
	}
}
