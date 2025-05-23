using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Sopra.Entities;
using Sopra.Responses;
using Sopra.Helpers;
using System.Diagnostics;

namespace Sopra.Services
{
    public interface IAuthService
    {
		User Authenticate(string email, string password, string ipAddress);
        Task<AuthResponse> UserAuthenticate(string email, string firebaseToken);
        Task<AuthenticationOTPRequest> AuthenticateOTP(string phone, string ipAddress);
		User AuthenticateVerifyOTP(string code, string ipAddress);
        // User ChangeProfile(string fullName, string password, long id);
        // IEnumerable<dynamic> GetRoles(long roleID);
        string GenerateToken(User user);
        // Task ForgetPassword(string email);
        // Task RecoveryPassword(string uniqueCode, string newPassword);
        // Task Register(string fullName, string email, string pass);
        // Task ResendActivationRegister(string email);
        // Task ResendActivationRegisterCheck(string email);
        // Task RegisterActivate(long uid);
        // Task<User> RegisterInvitation(long userId);
    }


}