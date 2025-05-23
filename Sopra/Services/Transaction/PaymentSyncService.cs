using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Sopra.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestSharp;
using Sopra.Entities;

namespace Sopra.Services
{
    public class PaymentSyncService
    {
        public static void Sync(EFContext context)
        {
            //define EFContext
            //var optionsBuilder = new DbContextOptionsBuilder<EFContext>();
            //optionsBuilder.UseSqlServer("Data Source=db1.mixtra.co.id\\SQLEXPRESS;Initial Catalog=SOPRA_STAGE;User ID=sopra;Password=Admin1234!;MultipleActiveResultSets=True;Encrypt=False");
            //optionsBuilder.EnableSensitiveDataLogging();
            //var context = new EFContext(optionsBuilder.Options);

            //define restClient
            var url = Utility.APIURL;
            var httpclient = new RestClient(url);

            using var dbTrans = context.Database.BeginTransaction();
            Trace.WriteLine("Running Sync Transaction Payment....");

            //get current datetime
            DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
            TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
            DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7

            //SYNC DATA FROM WEB TO MOBILE 
            try
            {
                var request = new RestRequest("/GetPaymentData", Method.Get);
                request.AddHeader("Content-Type", "application/json");
                var response = httpclient.Execute(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = JObject.Parse(response.Content);

                    // Access the "order" array inside the "response"
                    JArray jsonArray = (JArray)json["response"]["data"];
                    if (jsonArray != null && jsonArray.Count > 0)
                    {
                        for (int i = 0; i < jsonArray.Count; i++)
                        {
                            Trace.WriteLine($"----------------------------------------------------------------");
                            var checkExternalNo = context.Payments.FirstOrDefault(x => x.ExternalNo == Convert.ToString(jsonArray[i]["paymentNo"]));
                            if (checkExternalNo != null)
                            {
                                Trace.WriteLine($"ExternalOrderNo {checkExternalNo.ExternalNo} is exists");
                                context.Payments.Remove(checkExternalNo);
                                Trace.WriteLine($"ExternalOrderNo {checkExternalNo.ExternalNo} is updated");
                            }
                            //else
                            //{
                                var dataPayment = new Payment
                                {
                                    RefID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["refID"])) ? default(long) : (long)jsonArray[i]["refID"]),
                                    InvoicesID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["invoicesID"])) ? default(long) : (long)jsonArray[i]["invoicesID"]),
                                    PaymentNo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["paymentNo"])) ? default(string) : Convert.ToString(jsonArray[i]["paymentNo"])),
                                    Type = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["type"])) ? default(string) : Convert.ToString(jsonArray[i]["type"])),
                                    Netto = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["netto"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["netto"])),
                                    CustomersID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["customersID"])) ? default(long) : (long)jsonArray[i]["customersID"]),
                                    TransDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["transDate"])) ? null : Convert.ToDateTime(jsonArray[i]["transDate"])),
                                    Status = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["status"])) ? default(string) : Convert.ToString(jsonArray[i]["status"])),
                                    Note = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["note"])) ? default(string) : Convert.ToString(jsonArray[i]["note"])),
                                    BankRef = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["bankRef"])) ? default(string) : Convert.ToString(jsonArray[i]["bankRef"])),
                                    BankTime = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["bankTime"])) ? null : Convert.ToDateTime(jsonArray[i]["bankTime"])),
                                    AmtReceive = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["amtReceive"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["amtReceive"])),
                                    Username = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["username"])) ? default(string) : Convert.ToString(jsonArray[i]["username"])),
                                    //UsernameCancel = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["usernameCancel"])) ? default(string) : Convert.ToString(jsonArray[i]["usernameCancel"])),
                                    CompaniesID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["companiesID"])) ? default(long) : (long)jsonArray[i]["companiesID"]),
                                    ExternalNo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["paymentNo"])) ? default(string) : Convert.ToString(jsonArray[i]["paymentNo"])),
                                    DateIn = gmtPlus7Time,
                                    UserIn = 888,
                                };
                                context.Payments.Add(dataPayment);
                                Trace.WriteLine($"Successfully Synced ExternalNo {Convert.ToString(jsonArray[i]["paymentNo"])}");
                                Trace.WriteLine($"----------------------------------------------------------------");
                            //}
                            }
                        context.SaveChanges();
                        Trace.WriteLine($"Synchronization Transaction Payment completed successfully.\n");
                        dbTrans.Commit();
                        Trace.WriteLine($"commit db");
                    }
                    }
                
            }
            catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
        }
    }
}
