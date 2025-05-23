using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sopra.Services
{
    public class SnapBcaService : SnapBcaInterface
    {
        private readonly EFContext _context;

        public SnapBcaService(EFContext context)
        {
            _context = context;
        }

        public string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var token = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                token.Append(chars[random.Next(chars.Length)]);
            }
            return token.ToString();
        }

        public async Task<Response<object>> GetTokenAsync(string client_id, string client_secret, string public_key) 
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                
                string token = GenerateToken(32);
                //var credential = await _context.Credentials.AsNoTracking().FirstOrDefaultAsync(x => x.ClientId.Contains(client_id) && x.ClientSecret.Contains(client_secret));
                var credential = from a in _context.Credentials where a.ClientId == client_id && a.ClientSecret == client_secret && a.IsDeleted == false select a;
                

                if (credential == null)
                {
                    return null; 
                }


                dynamic msg = new System.Dynamic.ExpandoObject();
                    var data = new CredentialToken
                    {
                        Token = token,
                        CredentialsId = credential.Select(x => x.ID).FirstOrDefault(),
                        DateIn = DateTime.Now
                    };
                    
                    msg.responseCode = "2007300";
                    msg.responseMessage = "Successful";
                    msg.accessToken = token;
                    msg.tokenType = "Bearer";
                    msg.expiresIn = "900";
                    var response = new Response<object>
                    {
                        Data = msg
                    };

                await _context.CredentialTokens.AddAsync(data);
                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();
                

                return response;
            }
            catch (Exception ex)
            {
                    Trace.WriteLine(ex.Message);
                    if (ex.StackTrace != null)
                        Trace.WriteLine(ex.StackTrace);

                    await dbTrans.RollbackAsync();

                    return null;

            }

        }

        public Task<Response<object>> Inquiry() { return null; }
        public Task<Response<object>> Payment() { return null; }

        public bool VerifySignature(string stringToSign, byte[] decodedSignature, string public_key)
        {
            try
            {
                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportFromPem(public_key);

                    byte[] dataToVerify = Encoding.UTF8.GetBytes(stringToSign);
                    return rsa.VerifyData(dataToVerify, decodedSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying signature: {ex.Message}");
                return false;
            }
        }

        public string GetPublicKey()
        {
            //string filePath = "E:\\Karvin\\Kerja\\MIT\\Work\\SOPRA\\sopra-api\\Sopra\\public-key.pem"; // Replace this with the path to your file
            string filePath = "/public/public-key.pem"; // Replace this with the path to your file

            try
            {
                // Read all text from the file
                string fileContent = File.ReadAllText(filePath);

                // Print the file content to the console
                Console.WriteLine("File content:");
                Console.WriteLine(fileContent);
                return fileContent;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found.");
                return null;
            }
            catch (IOException e)
            {
                Console.WriteLine($"An error occurred while reading the file: {e.Message}");
                return null;
            }
            
        }

        public string GetPrivateKey()
        {
            string filePath = "E:\\Karvin\\Kerja\\MIT\\Work\\SOPRA\\sopra-api\\Sopra\\private-key.pem"; // Replace this with the path to your file
            //string filePath = "/public/private-key.pem"; // Replace this with the path to your file

            try
            {
                // Read all text from the file
                string fileContent = File.ReadAllText(filePath);

                // Print the file content to the console
                Console.WriteLine("File content:");
                Console.WriteLine(fileContent);
                return fileContent;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found.");
                return null;
            }
            catch (IOException e)
            {
                Console.WriteLine($"An error occurred while reading the file: {e.Message}");
                return null;
            }

        }

        public byte[] GetSign(string stringToSign, string private_key)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    // Import the private key
                    rsa.ImportFromPem(private_key);

                    // Create a SHA256 hash of the data
                    byte[] dataBytes = Encoding.UTF8.GetBytes(stringToSign);
                    byte[] hashBytes;
                    using (var sha256 = SHA256.Create())
                    {
                        hashBytes = sha256.ComputeHash(dataBytes);
                    }

                    // Sign the hash
                    byte[] signatureBytes = rsa.SignHash(hashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    return signatureBytes;
                }
            }
            catch (CryptographicException e)
            {
                Console.WriteLine($"Error signing data: {e.Message}");
                return null;
            }
        }

        public async Task<Credential> GetCredential(string client_id, string client_secret)
        {
            try
            {
                return await _context.Credentials.AsNoTracking().FirstOrDefaultAsync(x => x.ClientId == client_id && x.ClientSecret == client_secret);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
            
        }

        public async Task<CredentialToken> GetCredentialToken(string token)
        {
            try
            {
                return await _context.CredentialTokens.AsNoTracking().FirstOrDefaultAsync(x => x.Token == token);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<object> getDataInvoice(string customer_number,string va_number)
        {
            try
            {
                var data = null as object;
                data = await(
                        from i in _context.Invoices
                        join u in _context.Users on i.CustomersID equals u.RefID 
                        join p in _context.Payments on i.RefID equals p.InvoicesID into payment
                        from p in payment.DefaultIfEmpty()
                        where i.CustNum == customer_number
                         && i.VANum == va_number
                         && i.Status == "ACTIVE"
                         && i.FlagInv == 1
                        group new { i, u, p } by new { u.FirstName, u.LastName, i.CustNum, i.VANum, u.RefID, i.DateUp} into g
                        orderby g.Key.RefID ascending, g.Key.DateUp ascending
                        select new
                        {
                            g.Key.FirstName,
                            g.Key.LastName,
                            Netto = g.Sum(x => x.i.Netto),
                            Bill = g.Sum(x => x.i.Bill),
                            g.Key.CustNum,
                            g.Key.VANum,
                            g.Key.RefID,
                            g.Key.DateUp,
                            PaymentId = g.Key.RefID
                        }).FirstOrDefaultAsync<object>();

                return data;
                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public bool VerifySignature(string requestBody, string timestamp, string signature, string url, string token, string method,string client_secret)
        {
            method = method.ToUpper();
            string hash_hmac = "";
            byte[] hashBytesHMAC;
            byte[] hashBytesSHA;

            string hashed_lower_json;
            using (SHA256 sha256 = SHA256.Create())
            {
                hashBytesSHA = sha256.ComputeHash(Encoding.UTF8.GetBytes(requestBody.ToLower()));
                hashed_lower_json = BitConverter.ToString(hashBytesSHA).Replace("-", "").ToLower();
            }

            var stringToSign = method + ":" + url + ":" + token + ":" + client_secret + ":" + timestamp;

            // Convert client secret to byte array
            byte[] keyBytes = Encoding.UTF8.GetBytes(client_secret);

            // Convert string to sign to byte array
            byte[] dataBytes = Encoding.UTF8.GetBytes(stringToSign);
            

            using (HMACSHA512 hmac = new HMACSHA512(keyBytes))
            {
                // Compute the hash
                hashBytesHMAC = hmac.ComputeHash(dataBytes);

                // Convert the hash to a base64-encoded string
                hash_hmac = Convert.ToBase64String(hashBytesHMAC);
            }

            bool isSignatureValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(hash_hmac)
            );

            return isSignatureValid;
        }

        public void UpdateExternalID(string client_id, string client_secret,string externalid)
        {
            try
            {
                var data =_context.Credentials.AsNoTracking().FirstOrDefault(x => x.ClientId == client_id && x.ClientSecret == client_secret);
                data.ExternalId = externalid;
                data.DateUp = DateTime.Now;
                _context.Credentials.Update(data);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public bool CheckIsNumeric(string word)
        {
            Regex rNumeric = new Regex("^[0-9]*$");
            if (rNumeric.IsMatch(word)) return true;
            return false;
        }
    }
}
