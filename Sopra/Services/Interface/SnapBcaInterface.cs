using Sopra.Entities;
using Sopra.Responses;
using System;
using System.Threading.Tasks;

namespace Sopra.Services
{
    public interface SnapBcaInterface
    {
        Task<Response<object>> GetTokenAsync(string client_id, string client_secret, string public_key);
        Task<Response<object>> Inquiry();
        Task<Response<object>> Payment();
        Task<object> getDataInvoice(string customer_number, string va_number);
        Task<Credential> GetCredential(string client_id, string client_secret);
        Task<CredentialToken> GetCredentialToken(string token);
        bool CheckIsNumeric(string word);
        bool VerifySignature(string stringToSign, byte[] decodedSignature, string public_key);
        bool VerifySignature(string requestBody, string timestamp, string signature,string url,string token,string method, string client_secret);
        string GetPublicKey();
        string GetPrivateKey();
        void UpdateExternalID(string client_id, string client_secret,string externalid);
        byte[] GetSign(string stringToSign,string private_key);
    }
}
