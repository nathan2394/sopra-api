using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Sopra.Requests;
using System.Text;
using Sopra.Services;
using Microsoft.AspNetCore.Http;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.Xml;
using Azure.Core;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("/openapi/v1.0/")]

    public class SnapBcaController : ControllerBase
    {
        private readonly SnapBcaService _service;
        private string client_id;
        private string client_secret;
        private string public_key;
        private string private_key;

        public SnapBcaController(SnapBcaService service)
        {
            _service = service;
            client_id = "fc5a02b3-7f72-437a-810c-62a8adcd0e5b";
            client_secret = "bY9ummWm-75VW-JDyU-/jEE-cJ7yP929VydYjGfVViIPp/c=";
            public_key = _service.GetPublicKey();
            private_key = _service.GetPrivateKey();
        }

        //[Authorize]
        [HttpPost("access-token/b2b")]
        public async Task<IActionResult> GetToken([FromBody] SnapBcaGetToken obj,[FromQuery(Name = "X-TIMESTAMP")] string X_TIMESTAMP= "", [FromQuery(Name = "X-CLIENT-KEY")] string X_CLIENT_KEY = "", [FromQuery(Name = "X-SIGNATURE")] string X_SIGNATURE = "")
        {
            try
            {
                dynamic msg = new System.Dynamic.ExpandoObject();
                if  (X_CLIENT_KEY != client_id)
                {
                    msg.responseCode = "4017300";
                    msg.responseMessage = "Unauthorized. [Unknown client]";
                    return Unauthorized(msg);
                }

                if(X_TIMESTAMP == "")
                {
                    msg.responseCode = "4007301";
                    msg.responseMessage = "Invalid Field Format [X-TIMESTAMP]";
                    return BadRequest(msg);
                }

                DateTime formatTimestamp = DateTime.Parse(X_TIMESTAMP);

                // Get the difference in minutes between the current time and the parsed timestamp
                TimeSpan differenceInMinutes = DateTime.Now - formatTimestamp;

                // Get the total number of minutes in the difference
                int totalMinutesDifference = (int)differenceInMinutes.TotalMinutes;

                if(totalMinutesDifference >= 10)
                {
                    msg.responseCode = "4007301";
                    msg.responseMessage = "Invalid Field Format [X-TIMESTAMP]";
                    return BadRequest(msg);
                }

                if(obj.grantType != "client_credentials")
                {
                    msg.responseCode = "4007301";
                    msg.responseMessage = "Invalid Field Format [clientId/clientSecret/grantType]";
                    return BadRequest(msg);
                }

                var stringToSign = client_id + "|" + X_TIMESTAMP;

                //var signature = _service.GetSign(stringToSign, private_key);

                // Encode the bytes to Base64
                //string base64String = Convert.ToBase64String(signature);

                var decodedSignature = Convert.FromBase64String(X_SIGNATURE);

                bool isSignatureValid = _service.VerifySignature(stringToSign, decodedSignature, public_key);

                if (!isSignatureValid)
                {
                    msg.responseCode = "4017300";
                    msg.responseMessage = "Unauthorized. [Signature]";
                    return Unauthorized(msg);
                }

                var result = await _service.GetTokenAsync(client_id,client_secret, public_key);
                if(result == null)
                {
                    msg.error= "Couldn't Create Token";
                    return BadRequest(msg);
                }

                return Ok(result);
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
                Trace.WriteLine(message, "SnapBcaController");
                throw;
            }
        }

        //[Authorize]
        [HttpPost("transfer-va/inquiry")]
        public async Task<IActionResult> Invoice([FromBody] SnapBcaGetInquiry obj, [FromQuery(Name = "Authorization")] string auth = "", [FromQuery(Name = "CHANNEL-ID ")] string CHANNEL_ID = "", [FromQuery(Name = "X-PARTNER-ID")] string X_PARTNER_ID = "", [FromQuery(Name = "X-EXTERNAL-ID")] string X_EXTERNAL_ID = "", [FromQuery(Name = "X-TIMESTAMP")] string X_TIMESTAMP = "", [FromQuery(Name = "X-SIGNATURE")] string X_SIGNATURE = "")
        {
            try
            {
                dynamic msg = new System.Dynamic.ExpandoObject();
                var url = "/openapi/v1.0/transfer-va/inquiry";
                var method = "post";
                //string auth = HttpContext.Request.Headers["Authorization"];

                var credential = await _service.GetCredential(client_id, client_secret);
                var getToken = await _service.GetCredentialToken(auth.Replace("Bearer ", ""));

                if (auth != "Bearer " + getToken.Token)
                {
                    msg.responseCode = "4012401";
                    msg.responseMessage = "Invalid Token (B2B)";
                    return BadRequest(msg);
                }
                else
                {
                    // Parse the created_at string to a DateTime object
                    DateTime createdDateTime = DateTime.Parse(getToken.DateIn.ToString());

                    // Add 900 seconds (15 minutes) to the created_at time
                    DateTime expirationTime = createdDateTime.AddSeconds(900);

                    // Get the current time
                    DateTime currentTime = DateTime.Now;
                    if (currentTime > expirationTime)
                    {
                        msg.responseCode = "4012401";
                        msg.responseMessage = "Invalid Token (B2B)";
                        return BadRequest(msg);
                    }
                }

                if (CHANNEL_ID != "95231")
                {
                    msg.responseCode = "4012400";
                    msg.responseMessage = "Unauthorized. [Unknown client]";
                    return BadRequest(msg);
                }
                if (X_PARTNER_ID != "14767")
                {
                    msg.responseCode = "4012400";
                    msg.responseMessage = "Unauthorized. [Unknown client]";
                    return BadRequest(msg);
                }
                string requestBody = JsonConvert.SerializeObject(obj);
                bool isSignatureValid = _service.VerifySignature(requestBody, X_TIMESTAMP, X_SIGNATURE, url, getToken.Token, method, client_secret);
                if (!isSignatureValid)
                {
                    msg.responseCode = "4012400";
                    msg.responseMessage = "Unauthorized. [Signature]";
                    return BadRequest(msg);
                }
                var emptyObject = new { };

                var vaNumber = obj.virtualAccountNo;
                var partnerServiceId = obj.partnerServiceId;
                var CustomerNumber = obj.customerNo;
                var channelCode = obj.channelCode;
                var inquiryRequest = obj.inquiryRequestId;
                var additionalInfo = obj.additionalInfo is Newtonsoft.Json.Linq.JArray jArray && !jArray.HasValues
                ? emptyObject : obj.additionalInfo;

                dynamic reason = new { english = "Success", indonesia = "Sukses" };

                msg.responseCode = "2002400";
                msg.status = "200";
                msg.responseMessage = "Success";
                msg.inquiryStatus = "00";
                msg.inquiryReason = reason;

                var dataInvoice = await _service.getDataInvoice(CustomerNumber, vaNumber);
                dynamic inv;
                var custName="";
                var amount=0;
                var currency = "";
                var totalAmount = "";

                if (dataInvoice != null)
                {
                    inv = dataInvoice;

                    if (inv.Bill != null && inv.Bill != 0)
                    {
                        inv.Netto = inv.Bill;
                    }
                    amount = inv.Netto;
                    custName = inv.FirstName + " " + inv.LastName;
                }

                if(X_EXTERNAL_ID == credential.ExternalId)
                {
                    msg.responseCode = "4092400";
                    msg.status = "409";
                    msg.responseMessage = "Conflict";
                    msg.inquiryStatus = "01";
                    reason.english = "Cannot use the same X-EXTERNAL-ID";
                    reason.indonesia = "Tidak bisa menggunakan X-EXTERNAL-ID yang sama";
                    msg.inquiryReason = reason;
                } else
                {
                    _service.UpdateExternalID(client_id, client_secret, X_EXTERNAL_ID);
                    if(string.IsNullOrEmpty(partnerServiceId))
                    {
                        msg.responseCode = "4002402";
                        msg.status = "400";
                        msg.responseMessage = "Invalid Mandatory Field [partnerServiceId]";
                        msg.inquiryStatus = "01";
                        reason.english = "partnerServiceId is Empty";
                        reason.indonesia = "partnerServiceId Kosong";
                        msg.inquiryReason = reason;
                    } else if(_service.CheckIsNumeric(partnerServiceId) == false)
                    {
                        msg.inquiryStatus = "01";
                        reason.english = "partnerServiceId is Invalid";
                        reason.indonesia = "partnerServiceId Invalid";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002401";
                        msg.responseMessage = "Invalid Field Format [partnerServiceId]";   
                    } else if (string.IsNullOrEmpty(CustomerNumber))
                    {
                        CustomerNumber = "";
                        msg.inquiryStatus = "01";
                        reason.english = "customerNo is Empty";
                        reason.indonesia = "customerNo Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [customerNo]";
                    } else if(_service.CheckIsNumeric(CustomerNumber) == false)
                    {
                        msg.inquiryStatus = "01";
                        reason.english = "customerNo is Invalid";
                        reason.indonesia = "customerNo Invalid";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002401";
                        msg.responseMessage = "Invalid Field Format [customerNo]";
                    } else if (string.IsNullOrEmpty(vaNumber))
                    {
                        vaNumber = "";
                        msg.inquiryStatus = "01";
                        reason.english = "virtualAccountNo is Empty";
                        reason.indonesia = "virtualAccountNo Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [virtualAccountNo]";
                    } else if(_service.CheckIsNumeric(vaNumber) == false)
                    {
                        msg.inquiryStatus = "01";
                        reason.english = "virtualAccountNo is Invalid";
                        reason.indonesia = "virtualAccountNo Invalid";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002401";
                        msg.responseMessage = "Invalid Field Format [virtualAccountNo]";
                    } else if (channelCode == 0)
                    {
                        channelCode = 0;
                        msg.inquiryStatus = "01";
                        reason.english = "channelCode is Empty";
                        reason.indonesia = "channelCode Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [channelCode]";
                    } else if (string.IsNullOrEmpty(inquiryRequest))
                    {
                        inquiryRequest = "";
                        msg.inquiryStatus = "01";
                        reason.english = "inquiryRequestId is Empty";
                        reason.indonesia = "inquiryRequestId Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [inquiryRequestId]";
                    }

                    if(msg.inquiryStatus == "00")
                    {
                        inv = dataInvoice;
                        if (dataInvoice == null)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "Bill not found";
                            reason.indonesia = "Tagihan tidak ditemukan";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042412";
                            msg.responseMessage = "Invalid Bill/Virtual Account [Not Found]";
                        } else if(inv.PaymentId != 0)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "Bill has been paid";
                            reason.indonesia = "Tagihan telah dibayar";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042414";
                            msg.responseMessage = "Paid Bill";
                        } 
                        
                        if(inv.Netto == 0 || amount == 0)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "Bill not found";
                            reason.indonesia = "Tagihan tidak ditemukan";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042412";
                            msg.responseMessage = "Bill not found";
                        } else
                        {
                            currency = "IDR";
                            totalAmount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                        }

                        var expDate = DateTime.Parse(inv.DateUp);
                        var currentDate = DateTime.Today;
                        expDate = expDate.AddDays(1);
                        if(currentDate > expDate)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "VA expired";
                            reason.indonesia = "VA kadaluarsa";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042419";
                            msg.responseMessage = "Bill expired";
                        }
                    }
                }

                dynamic amountTotal= new { value = totalAmount, currency = currency };
                msg.totalAmount = amountTotal;

                dynamic response = new System.Dynamic.ExpandoObject();
                response.responseCode = msg.responseCode;
                response.responseMessage = msg.responseMessage;

                dynamic virtualAccountData = new System.Dynamic.ExpandoObject();
                virtualAccountData.inquiryStatus = msg.inquiryStatus;
                virtualAccountData.inquiryReason = msg.inquiryReason;
                virtualAccountData.partnerServiceId = "   " + partnerServiceId;
                virtualAccountData.customerNo = CustomerNumber;
                virtualAccountData.virtualAccountNo = "   " + vaNumber;
                virtualAccountData.virtualAccountName = custName;
                virtualAccountData.virtualAccountEmail = "";
                virtualAccountData.virtualAccountPhone = "";
                virtualAccountData.inquiryRequestId = inquiryRequest;
                virtualAccountData.totalAmount = amountTotal;
                virtualAccountData.subCompany = "00000";

                virtualAccountData.billDetails = new System.Dynamic.ExpandoObject[0]; // empty array
                
                dynamic freeTexts = new System.Dynamic.ExpandoObject();
                freeTexts.english = "Free text";
                freeTexts.indonesia = "Tulisan bebas";

                dynamic[] freeTextArray = new dynamic[] { freeTexts };
                virtualAccountData.freeTexts = freeTextArray;
                virtualAccountData.virtualAccountTrxType = "C";

                dynamic feeAmount = new System.Dynamic.ExpandoObject();
                feeAmount.value = "";
                feeAmount.currency = "";
                virtualAccountData.feeAmount = feeAmount;

                virtualAccountData.additionalInfo = additionalInfo;

                response.virtualAccountData = virtualAccountData;
                return StatusCode(msg.status, response);
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
                Trace.WriteLine(message, "SnapBcaController");
                throw;
            }
        }

        //[Authorize]
        [HttpPost("transfer-va/payment")]
        public async Task<IActionResult> Payment([FromBody] SnapBcaGetInquiry obj, [FromQuery(Name = "Authorization")] string auth = "", [FromQuery(Name = "CHANNEL-ID ")] string CHANNEL_ID = "", [FromQuery(Name = "X-PARTNER-ID")] string X_PARTNER_ID = "", [FromQuery(Name = "X-EXTERNAL-ID")] string X_EXTERNAL_ID = "", [FromQuery(Name = "X-TIMESTAMP")] string X_TIMESTAMP = "", [FromQuery(Name = "X-SIGNATURE")] string X_SIGNATURE = "")
        {
            try
            {
                dynamic msg = new System.Dynamic.ExpandoObject();
                var url = "/openapi/v1.0/transfer-va/payment";
                var method = "post";
                //string auth = HttpContext.Request.Headers["Authorization"];

                var credential = await _service.GetCredential(client_id, client_secret);
                var getToken = await _service.GetCredentialToken(auth.Replace("Bearer ", ""));

                if (auth != "Bearer " + getToken.Token)
                {
                    msg.responseCode = "4012401";
                    msg.responseMessage = "Invalid Token (B2B)";
                    return BadRequest(msg);
                }
                else
                {
                    // Parse the created_at string to a DateTime object
                    DateTime createdDateTime = DateTime.Parse(getToken.DateIn.ToString());

                    // Add 900 seconds (15 minutes) to the created_at time
                    DateTime expirationTime = createdDateTime.AddSeconds(900);

                    // Get the current time
                    DateTime currentTime = DateTime.Now;
                    if (currentTime > expirationTime)
                    {
                        msg.responseCode = "4012401";
                        msg.responseMessage = "Invalid Token (B2B)";
                        return BadRequest(msg);
                    }
                }

                if (CHANNEL_ID != "95231")
                {
                    msg.responseCode = "4012400";
                    msg.responseMessage = "Unauthorized. [Unknown client]";
                    return BadRequest(msg);
                }
                if (X_PARTNER_ID != "14767")
                {
                    msg.responseCode = "4012400";
                    msg.responseMessage = "Unauthorized. [Unknown client]";
                    return BadRequest(msg);
                }
                string requestBody = JsonConvert.SerializeObject(obj);
                bool isSignatureValid = _service.VerifySignature(requestBody, X_TIMESTAMP, X_SIGNATURE, url, getToken.Token, method, client_secret);
                if (!isSignatureValid)
                {
                    msg.responseCode = "4012400";
                    msg.responseMessage = "Unauthorized. [Signature]";
                    return BadRequest(msg);
                }
                var emptyObject = new { };

                var vaNumber = obj.virtualAccountNo;
                var partnerServiceId = obj.partnerServiceId;
                var CustomerNumber = obj.customerNo;
                var channelCode = obj.channelCode;
                var inquiryRequest = obj.inquiryRequestId;
                var additionalInfo = obj.additionalInfo is Newtonsoft.Json.Linq.JArray jArray && !jArray.HasValues
                ? emptyObject : obj.additionalInfo;

                dynamic reason = new { english = "Success", indonesia = "Sukses" };

                msg.responseCode = "2002400";
                msg.status = "200";
                msg.responseMessage = "Success";
                msg.inquiryStatus = "00";
                msg.inquiryReason = reason;

                var dataInvoice = await _service.getDataInvoice(CustomerNumber, vaNumber);
                dynamic inv;
                var custName = "";
                var amount = 0;
                var currency = "";
                var totalAmount = "";

                if (dataInvoice != null)
                {
                    inv = dataInvoice;

                    if (inv.Bill != null && inv.Bill != 0)
                    {
                        inv.Netto = inv.Bill;
                    }
                    amount = inv.Netto;
                    custName = inv.FirstName + " " + inv.LastName;
                }

                if (X_EXTERNAL_ID == credential.ExternalId)
                {
                    msg.responseCode = "4092400";
                    msg.status = "409";
                    msg.responseMessage = "Conflict";
                    msg.inquiryStatus = "01";
                    reason.english = "Cannot use the same X-EXTERNAL-ID";
                    reason.indonesia = "Tidak bisa menggunakan X-EXTERNAL-ID yang sama";
                    msg.inquiryReason = reason;
                }
                else
                {
                    _service.UpdateExternalID(client_id, client_secret, X_EXTERNAL_ID);
                    if (string.IsNullOrEmpty(partnerServiceId))
                    {
                        msg.responseCode = "4002402";
                        msg.status = "400";
                        msg.responseMessage = "Invalid Mandatory Field [partnerServiceId]";
                        msg.inquiryStatus = "01";
                        reason.english = "partnerServiceId is Empty";
                        reason.indonesia = "partnerServiceId Kosong";
                        msg.inquiryReason = reason;
                    }
                    else if (_service.CheckIsNumeric(partnerServiceId) == false)
                    {
                        msg.inquiryStatus = "01";
                        reason.english = "partnerServiceId is Invalid";
                        reason.indonesia = "partnerServiceId Invalid";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002401";
                        msg.responseMessage = "Invalid Field Format [partnerServiceId]";
                    }
                    else if (string.IsNullOrEmpty(CustomerNumber))
                    {
                        CustomerNumber = "";
                        msg.inquiryStatus = "01";
                        reason.english = "customerNo is Empty";
                        reason.indonesia = "customerNo Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [customerNo]";
                    }
                    else if (_service.CheckIsNumeric(CustomerNumber) == false)
                    {
                        msg.inquiryStatus = "01";
                        reason.english = "customerNo is Invalid";
                        reason.indonesia = "customerNo Invalid";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002401";
                        msg.responseMessage = "Invalid Field Format [customerNo]";
                    }
                    else if (string.IsNullOrEmpty(vaNumber))
                    {
                        vaNumber = "";
                        msg.inquiryStatus = "01";
                        reason.english = "virtualAccountNo is Empty";
                        reason.indonesia = "virtualAccountNo Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [virtualAccountNo]";
                    }
                    else if (_service.CheckIsNumeric(vaNumber) == false)
                    {
                        msg.inquiryStatus = "01";
                        reason.english = "virtualAccountNo is Invalid";
                        reason.indonesia = "virtualAccountNo Invalid";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002401";
                        msg.responseMessage = "Invalid Field Format [virtualAccountNo]";
                    }
                    else if (channelCode == 0)
                    {
                        channelCode = 0;
                        msg.inquiryStatus = "01";
                        reason.english = "channelCode is Empty";
                        reason.indonesia = "channelCode Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [channelCode]";
                    }
                    else if (string.IsNullOrEmpty(inquiryRequest))
                    {
                        inquiryRequest = "";
                        msg.inquiryStatus = "01";
                        reason.english = "inquiryRequestId is Empty";
                        reason.indonesia = "inquiryRequestId Kosong";
                        msg.inquiryReason = reason;
                        msg.status = "400";
                        msg.responseCode = "4002402";
                        msg.responseMessage = "Invalid Mandatory Field [inquiryRequestId]";
                    }

                    if (msg.inquiryStatus == "00")
                    {
                        inv = dataInvoice;
                        if (dataInvoice == null)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "Bill not found";
                            reason.indonesia = "Tagihan tidak ditemukan";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042412";
                            msg.responseMessage = "Invalid Bill/Virtual Account [Not Found]";
                        }
                        else if (inv.PaymentId != 0)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "Bill has been paid";
                            reason.indonesia = "Tagihan telah dibayar";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042414";
                            msg.responseMessage = "Paid Bill";
                        }

                        if (inv.Netto == 0 || amount == 0)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "Bill not found";
                            reason.indonesia = "Tagihan tidak ditemukan";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042412";
                            msg.responseMessage = "Bill not found";
                        }
                        else
                        {
                            currency = "IDR";
                            totalAmount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                        }

                        var expDate = DateTime.Parse(inv.DateUp);
                        var currentDate = DateTime.Today;
                        expDate = expDate.AddDays(1);
                        if (currentDate > expDate)
                        {
                            msg.inquiryStatus = "01";
                            reason.english = "VA expired";
                            reason.indonesia = "VA kadaluarsa";
                            msg.inquiryReason = reason;
                            msg.status = "404";
                            msg.responseCode = "4042419";
                            msg.responseMessage = "Bill expired";
                        }
                    }
                }

                dynamic amountTotal = new { value = totalAmount, currency = currency };
                msg.totalAmount = amountTotal;

                dynamic response = new System.Dynamic.ExpandoObject();
                response.responseCode = msg.responseCode;
                response.responseMessage = msg.responseMessage;

                dynamic virtualAccountData = new System.Dynamic.ExpandoObject();
                virtualAccountData.inquiryStatus = msg.inquiryStatus;
                virtualAccountData.inquiryReason = msg.inquiryReason;
                virtualAccountData.partnerServiceId = "   " + partnerServiceId;
                virtualAccountData.customerNo = CustomerNumber;
                virtualAccountData.virtualAccountNo = "   " + vaNumber;
                virtualAccountData.virtualAccountName = custName;
                virtualAccountData.virtualAccountEmail = "";
                virtualAccountData.virtualAccountPhone = "";
                virtualAccountData.inquiryRequestId = inquiryRequest;
                virtualAccountData.totalAmount = amountTotal;
                virtualAccountData.subCompany = "00000";

                virtualAccountData.billDetails = new System.Dynamic.ExpandoObject[0]; // empty array

                dynamic freeTexts = new System.Dynamic.ExpandoObject();
                freeTexts.english = "Free text";
                freeTexts.indonesia = "Tulisan bebas";

                dynamic[] freeTextArray = new dynamic[] { freeTexts };
                virtualAccountData.freeTexts = freeTextArray;
                virtualAccountData.virtualAccountTrxType = "C";

                dynamic feeAmount = new System.Dynamic.ExpandoObject();
                feeAmount.value = "";
                feeAmount.currency = "";
                virtualAccountData.feeAmount = feeAmount;

                virtualAccountData.additionalInfo = additionalInfo;

                response.virtualAccountData = virtualAccountData;
                return StatusCode(msg.status, response);
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
                Trace.WriteLine(message, "SnapBcaController");
                throw;
            }
        }

    }
}
