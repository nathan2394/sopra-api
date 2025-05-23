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
using System.Globalization;

namespace Sopra.Services
{
    public class InvoiceSyncService
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
            Trace.WriteLine("Running Sync Transaction Invoice....");
            var data = Utility.SQLGetObjects(string.Format("select * from Invoices  where DateIn BETWEEN '{0:yyyy-MM-dd HH:mm:ss}' and CONCAT(cast(GETDATE() as date) ,' 23:59:59') and ExternalNo is null", Utility.TransactionSyncDate), Utility.SQLDBConnection);

            //get current datetime
            DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
            TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
            DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7
            //SYNC DATA FROM WEB TO MOBILE 
            try
            {
                var request = new RestRequest("/GetInvoiceData", Method.Get);
                request.AddHeader("Content-Type", "application/json");
                var response = httpclient.Execute(request);
                if (response.IsSuccessStatusCode)
                {
                    // Parse the JSON string
                    var json = JObject.Parse(response.Content);

                    // Access the "order" array inside the "response"
                    JArray jsonArray = (JArray)json["response"]["data"];
                    if (jsonArray != null && jsonArray.Count > 0)
                    {
                        for (int i = 0; i < jsonArray.Count; i++)
                        {
                            Trace.WriteLine($"----------------------------------------------------------------");
                            var checkExternalNo = context.Invoices.FirstOrDefault(x => x.ExternalNo == Convert.ToString(jsonArray[i]["invoiceNo"]));
                            if (checkExternalNo != null)
                            {
                                Trace.WriteLine($"ExternalNo {checkExternalNo.ExternalNo} is exists");
                                context.Invoices.Remove(checkExternalNo);
                                Trace.WriteLine($"ExternalOrderNo {checkExternalNo.ExternalNo} is updated");
                            }
                            //else
                            //{
                                var dataInvoice = new Invoice
                                {
                                    RefID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    OrdersID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["ordersID"])) ? default(long) : (long)jsonArray[i]["ordersID"]),
                                    PaymentMethod = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["paymentMethod"])) ? default(long) : (long)jsonArray[i]["paymentMethod"]),
                                    InvoiceNo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["invoiceNo"])) ? default(string) : Convert.ToString(jsonArray[i]["invoiceNo"])),
                                    Type = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["type"])) ? default(string) : Convert.ToString(jsonArray[i]["type"])),
                                    Netto = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["netto"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["netto"])),
                                    CustomersID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["customersID"])) ? default(long) : (long)jsonArray[i]["customersID"]),
                                    TransDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["transDate"])) ? null : (DateTime.TryParseExact(jsonArray[i]["transDate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ? parsedDate : (DateTime?)null)),
                                    Status = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["status"])) ? default(string) : Convert.ToString(jsonArray[i]["status"])),
                                    VANum = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["vaNum"])) ? default(string) : Convert.ToString(jsonArray[i]["vaNum"])),
                                    CustNum = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["custNum"])) ? default(string) : Convert.ToString(jsonArray[i]["custNum"])),
                                    FlagInv = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["flagInv"])) ? default(long) : (long)jsonArray[i]["flagInv"]),
                                    ReasonsID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["reasonsID"])) ? default(long) : (long)jsonArray[i]["reasonsID"]),
                                    Note = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["note"])) ? default(string) : Convert.ToString(jsonArray[i]["note"])),
                                    Refund = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["refund"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["refund"])),
                                    Bill = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["bill"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["bill"])),
                                    PICInv = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["picInv"])) ? default(long) : (long)jsonArray[i]["picInv"]),
                                    BankName = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["bankName"])) ? default(string) : Convert.ToString(jsonArray[i]["bankName"])),
                                    AccountNumber = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["accountNumber"])) ? default(string) : Convert.ToString(jsonArray[i]["accountNumber"])),
                                    AccountName = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["accountName"])) ? default(string) : Convert.ToString(jsonArray[i]["accountName"])),
                                    TransferDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["transferDate"])) ? null : Convert.ToDateTime(jsonArray[i]["transferDate"])),
                                    BankRef = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["bankRef"])) ? default(string) : Convert.ToString(jsonArray[i]["bankRef"])),
                                    TransferAmount = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["transferAmount"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["transferAmount"])),
                                    Username = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["username"])) ? default(string) : Convert.ToString(jsonArray[i]["username"])),
                                    UsernameCancel = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["usernameCancel"])) ? default(string) : Convert.ToString(jsonArray[i]["usernameCancel"])),
                                    XenditID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["xenditID"])) ? default(long) : (long)jsonArray[i]["xenditID"]),
                                    XenditBank = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["xenditBank"])) ? default(string) : Convert.ToString(jsonArray[i]["xenditBank"])),
                                    PDFFile = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["pdfFile"])) ? default(string) : Convert.ToString(jsonArray[i]["pdfFile"])),
                                    DueDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["dueDate"])) ? null : Convert.ToDateTime(jsonArray[i]["dueDate"])),
                                    CompaniesID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["companiesID"])) ? default(long) : (long)jsonArray[i]["companiesID"]),
                                    SentWaCounter = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["sentWaCounter"])) ? default(long) : (long)jsonArray[i]["sentWaCounter"]),
                                    WASentTime = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["waSentTime"])) ? null : Convert.ToDateTime(jsonArray[i]["waSentTime"])),
                                    FutureDateStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["futureDateStatus"])) ? default(bool) : Convert.ToBoolean(jsonArray[i]["futureDateStatus"])),
                                    CreditStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["creditStatus"])) ? default(bool) : Convert.ToBoolean(jsonArray[i]["creditStatus"])),
                                    ExternalNo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["invoiceNo"])) ? default(string) : Convert.ToString(jsonArray[i]["invoiceNo"])),
                                    DateIn = gmtPlus7Time,
                                    UserIn = 888,
                                };
                                context.Invoices.Add(dataInvoice);
                                Trace.WriteLine($"Successfully Synced ExternalNo {Convert.ToString(jsonArray[i]["invoiceNo"])}");
                                Trace.WriteLine($"----------------------------------------------------------------");
                            //}
                        }
                    }
                    context.SaveChanges();
                    //Trace.WriteLine($"Synchronization Transaction Invoice completed successfully.\n");
                    //dbTrans.Commit();
                    //Trace.WriteLine($"commit db");
                }
            }

            catch (Exception ex)
            {
                Trace.WriteLine($"Error during synchronization: {ex.Message}");
            }

            //SYNC DATA FROM MOBILE TO WEB
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented
                };

                Trace.WriteLine($"get data from sql server database");

                if (data != null)
                {
                    List<Dictionary<string, object>> rowsList = new List<Dictionary<string, object>>();
                    foreach (DataRow row in data.Rows)
                    {
                        Dictionary<string, object> rowDict = new Dictionary<string, object>();
                        foreach (DataColumn column in row.Table.Columns)
                        {
                            rowDict[column.ColumnName] = row[column];
                        }
                        rowsList.Add(rowDict);
                    }

                    string jsonString = JsonConvert.SerializeObject(rowsList, settings);
                    JObject jsonData = JObject.Parse("{ \"invoice\": " + jsonString + " }");
                    Trace.WriteLine($"data invoice = {jsonData}");
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);

                    try
                    {
                        var request = new RestRequest("/Invoice", Method.Post);
                        request.AddParameter("application/json", json, ParameterType.RequestBody);
                        var response = httpclient.Execute(request);

                        Trace.WriteLine($"send data reguler into web");
                        var responseContent = response.Content;

                        if (response.IsSuccessStatusCode)
                        {
                            Trace.WriteLine($"send data successfully");
                            Trace.WriteLine($"content : {responseContent}");
                        }
                        else
                        {
                            Trace.WriteLine($"send data failed with status code: {response.StatusCode}");
                            Trace.WriteLine($"content : {responseContent}");

                            dbTrans.Rollback();
                            Trace.WriteLine($"rollback db");

                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error during synchronization: {ex.Message}");
                        dbTrans.Rollback();
                        Trace.WriteLine($"rollback db");
                    }

                    Trace.WriteLine($"Synchronization Transaction Invoice completed successfully.\n");
                }
                else Trace.WriteLine($"No Data.");

                dbTrans.Commit();
                Trace.WriteLine($"commit db");
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error during synchronization: {ex.Message}");
                dbTrans.Rollback();
                Trace.WriteLine($"rollback db");
                Thread.Sleep(100);
            }
        }
    }
}
