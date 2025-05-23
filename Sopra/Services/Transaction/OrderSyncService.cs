using System;
using System.Diagnostics;
//using System.Linq;
using System.Threading;
using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;

using Sopra.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Razor.TagHelpers;
using RestSharp;
using Sopra.Entities;
using Google.Api;

namespace Sopra.Services
{
    public class OrderSyncService
    {

        public static void Sync(EFContext context)
        {
            ////define EFContext
            //var optionsBuilder = new DbContextOptionsBuilder<EFContext>();
            //optionsBuilder.UseSqlServer("Data Source=db1.mixtra.co.id\\SQLEXPRESS;Initial Catalog=SOPRA_STAGE;User ID=sopra;Password=Admin1234!;MultipleActiveResultSets=True;Encrypt=False");
            //optionsBuilder.EnableSensitiveDataLogging();
            //var context = new EFContext(optionsBuilder.Options);

            //define restClient
            var url = Utility.APIURL;
            var httpclient = new RestClient(url);

            using var dbTrans = context.Database.BeginTransaction();
            Trace.WriteLine("Running Sync Transaction Order....");

            var data = Utility.SQLGetObjects(string.Format("select * from Orders where (DateIn BETWEEN '{0:yyyy-MM-dd HH:mm:ss}' and CONCAT(cast(GETDATE() as date) ,' 23:59:59') and OrderStatus = 'ACTIVE') and ExternalOrderNo is null and (TotalMix = 0 AND TotalJumbo = 0) or (DateIn BETWEEN DATEADD(day, -1, GETDATE()) and CONCAT(cast(GETDATE() as date) ,' 23:59:59') and OrderStatus = 'CANCEL')", Utility.TransactionSyncDate), Utility.SQLDBConnection);
            var dataPromo = Utility.SQLGetObjects(string.Format("select * from Orders where (DateIn BETWEEN '{0:yyyy-MM-dd HH:mm:ss}' and CONCAT(cast(GETDATE() as date) ,' 23:59:59') and OrderStatus = 'ACTIVE') and ExternalOrderNo is null and (TotalMix > 0 OR TotalJumbo > 0) or (DateIn BETWEEN DATEADD(day, -1, GETDATE()) and CONCAT(cast(GETDATE() as date) ,' 23:59:59') and OrderStatus = 'CANCEL')", Utility.TransactionSyncDate), Utility.SQLDBConnection);

            //get current datetime
            DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
            TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
            DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7


            //SYNC DATA FROM WEB TO MOBILE 
            try 
            {
                var cartNotZero = false;
                var cartTemp = Convert.ToInt64(0);
                var request = new RestRequest("/GetOrderData", Method.Get);
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
                            var cartid = string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["cartsID"])) ? 0 : Convert.ToInt64(jsonArray[i]["cartsID"]);
                            if(cartid == 0)
                            {
                                if (!cartNotZero && cartTemp == 0)
                                {
                                    var lastCartId = context.Orders.OrderByDescending(x => x.CartsID).FirstOrDefault();
                                    cartid = Convert.ToInt64(lastCartId.CartsID) + 1;
                                    cartTemp = cartid;
                                } else cartTemp += 1;
                                cartNotZero = true;
                            } else cartNotZero = false;

                            var checkAmountSame = false;
                            var amount = Convert.ToInt64(0);
                            var transdate = default(DateTime);
                            var customerid = string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["customersID"])) ? 0 : (long)jsonArray[i]["customersID"];
                            var checkOrderExternalNo = context.Orders.FirstOrDefault(x => x.ExternalOrderNo == Convert.ToString(jsonArray[i]["orderNo"]) 
                                                                                    || x.OrderNo == Convert.ToString(jsonArray[i]["mobileVoucherNo"])
                                                                                    && x.CustomersID == customerid
                                                                                    );
                            var paymentID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["paymentID"])) ? 0 : Convert.ToInt64(jsonArray[i]["paymentID"]));
                            var flagMobileIsDelete = false;
                            if (checkOrderExternalNo != null) 
                            {
                                if (checkOrderExternalNo.OrderNo != null && checkOrderExternalNo.OrderNo != "")
                                {
                                    transdate = Convert.ToDateTime(checkOrderExternalNo.TransDate);
                                    Trace.WriteLine($"OrderNo {checkOrderExternalNo.OrderNo} is exists");
                                    //context.Orders.Remove(checkOrderExternalNo);
                                    //Trace.WriteLine($"OrderNo {checkOrderExternalNo.OrderNo} is updated");
                                    flagMobileIsDelete = true;
                                }
                                else
                                {
                                    Trace.WriteLine($"ExternalOrderNo {checkOrderExternalNo.ExternalOrderNo} is exists");
                                    //context.Orders.Remove(checkOrderExternalNo);
                                    //Trace.WriteLine($"ExternalOrderNo {checkOrderExternalNo.ExternalOrderNo} is updated");
                                }
                            }
                            //else
                            //{
                                var dataOrder = new Order
                                {
                                    RefID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    OrderNo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["orderNo"])) ? default(string) : (flagMobileIsDelete == true ? Convert.ToString(jsonArray[i]["mobileVoucherNo"]) : Convert.ToString(jsonArray[i]["orderNo"]) )),
                                    TransDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["transDate"])) ? null : (flagMobileIsDelete == true ? transdate : Convert.ToDateTime(jsonArray[i]["transDate"]) )),
                                    CustomersID = customerid,
                                    ReferenceNo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["referenceNo"])) ? default(string) : Convert.ToString(jsonArray[i]["referenceNo"])),
                                    Other = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["other"])) ? default(string) : Convert.ToString(jsonArray[i]["other"])),
                                    Amount = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["amount"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["amount"])),
                                    Status = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["status"])) ? default(string) : Convert.ToString(jsonArray[i]["status"])),
                                    VouchersID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["vouchersID"])) ? default(string) : Convert.ToString(jsonArray[i]["vouchersID"])),
                                    Disc1 = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["disc1"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["disc1"])),
                                    Disc1Value = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["disc1Value"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["disc1Value"])),
                                    Disc2 = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["disc2"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["disc2"])),
                                    Disc2Value = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["disc2Value"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["disc2Value"])),
                                    Sfee = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["sfee"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["sfee"])),
                                    DPP = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["dpp"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["dpp"])),
                                    TAX = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["tax"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["tax"])),
                                    TaxValue = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["taxValue"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["taxValue"])),
                                    Total = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["total"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["total"])),
                                    //Departure = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //Arrival = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //WarehouseID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //CountriesID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //ProvincesID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //RegenciesID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //DistrictsID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    Address = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["address"])) ? default(string) : Convert.ToString(jsonArray[i]["address"])),
                                    Label = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["label"])) ? default(string) : Convert.ToString(jsonArray[i]["label"])),
                                    Landmark = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["landmark"])) ? default(string) : Convert.ToString(jsonArray[i]["landmark"])),
                                    //PostalCode = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"])),
                                    TransportsID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["transportsID"])) ? default(long) : (long)jsonArray[i]["transportsID"]),
                                    //TotalTransport = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //TotalTransportCapacity = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    TotalOrderCapacity = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["totalOrderCapacity"])) ? 0 : Convert.ToDecimal(jsonArray[i]["totalOrderCapacity"])),
                                    TotalOrderWeight = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["totalOrderWeight"])) ? 0 : Convert.ToDecimal(jsonArray[i]["totalOrderWeight"])),
                                    //TotalTransportCost = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //RemainingCapacity = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"])),
                                    ReasonsID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["reasonsID"])) ? default(long) : (long)jsonArray[i]["reasonsID"]),
                                    OrderStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["orderStatus"])) ? default(string) : Convert.ToString(jsonArray[i]["orderStatus"])),
                                    ExpeditionsID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["expeditionsID"])) ? default(long) : (long)jsonArray[i]["expeditionsID"]),
                                    BiayaPickup = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["biayaPickup"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["biayaPickup"])),
                                    CheckInvoice = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["checkInvoice"])) ? default(long) : (long)jsonArray[i]["checkInvoice"]),
                                    InvoicedDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["invoicedDate"])) ? null : Convert.ToDateTime(jsonArray[i]["invoicedDate"])),
                                    TotalReguler = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["totalReguler"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["totalReguler"])),
                                    TotalJumbo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["totalJumbo"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["totalJumbo"])),
                                    TotalMix = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["totalMix"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["totalMix"])),
                                    TotalNewPromo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["totalNewPromo"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["totalNewPromo"])),
                                    TotalSupersale = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["totalSupersale"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["totalSupersale"])),
                                    //ValidTime = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    DealerTier = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["dealerTier"])) ? default(string) : Convert.ToString(jsonArray[i]["dealerTier"])),
                                    //DeliveryStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //PartialDeliveryStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //PaymentTerm = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //Validity = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    IsVirtual = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["isVirtual"])) ? default(long) : (long)jsonArray[i]["isVirtual"]),
                                    VirtualAccount = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["virtualAccount"])) ? default(string) : Convert.ToString(jsonArray[i]["virtualAccount"])),
                                    //BanksID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //ShipmentNum = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    PaidDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["paidDate"])) ? null : Convert.ToDateTime(jsonArray[i]["paidDate"])),
                                    RecreateOrderStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["recreateOrderStatus"])) ? default(long) : (long)jsonArray[i]["recreateOrderStatus"]),
                                    CompaniesID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["companiesID"])) ? default(long) : (long)jsonArray[i]["companiesID"]),
                                    AmountTotal = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["amountTotal"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["amountTotal"])),
                                    //FutureDateStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //ChangeExpeditionStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //ChangetruckStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //SubscriptionCount = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //SubscriptionStatus = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //SubscriptionDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    Username = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["username"])) ? default(string) : Convert.ToString(jsonArray[i]["username"])),
                                    UsernameCancel = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["usernameCancel"])) ? default(string) : Convert.ToString(jsonArray[i]["usernameCancel"])),
                                    //SessionID = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    //SessionDate = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : (long)jsonArray[i]["id"]),
                                    ExternalOrderNo = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["orderNo"])) ? default(string) : Convert.ToString(jsonArray[i]["orderNo"])),
                                    DateIn = gmtPlus7Time,
                                    UserIn=888,
                                    CartsID = Convert.ToInt64(cartid),
                                    Type = (string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["type"])) ? default(string) : Convert.ToString(jsonArray[i]["type"])),
                                };
                                JArray jsonArrayDetail = (JArray)jsonArray[i]["orderDetail"];
                                if (jsonArrayDetail != null && jsonArrayDetail.Count > 0)
                                {
                                    var checkOrderDetail = context.OrderDetails.Where(x => x.OrdersID == dataOrder.RefID).ToList();
                                    if (checkOrderDetail.Count > 0)
                                    {
                                        Trace.WriteLine($"OrderDetail with {dataOrder.RefID} OrdersID is exists");
                                        context.OrderDetails.RemoveRange(checkOrderDetail);
                                        Trace.WriteLine($"OrderDetail with {dataOrder.RefID} OrdersID is updated");
                                    }
                                    List<OrderDetail> dataList = new List<OrderDetail>();

                                    for (int j = 0; j < jsonArrayDetail.Count; j++)
                                    {
                                        var dataOrderDetail = new OrderDetail
                                        {
                                            RefID = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["id"])) ? default(long) : (long)jsonArrayDetail[j]["id"]),
                                            OrdersID = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["ordersID"])) ? default(long) : (long)jsonArrayDetail[j]["ordersID"]),
                                            ObjectID = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["objectID"])) ? default(long) : (long)jsonArrayDetail[j]["objectID"]),
                                            ParentID = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["parentID"])) ? default(long) : (long)jsonArrayDetail[j]["parentID"]),
                                            ObjectType = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["objectType"])) ? default(string) : Convert.ToString(jsonArrayDetail[j]["objectType"])),
                                            PromosID = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["promosID"])) ? default(long) : (long)jsonArrayDetail[j]["promosID"]),
                                            Type = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["type"])) ? default(string) : Convert.ToString(jsonArrayDetail[j]["type"])),
                                            QtyBox = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["qtyBox"])) ? default(decimal) : Convert.ToDecimal(jsonArrayDetail[j]["qtyBox"])),
                                            Qty = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["qty"])) ? default(decimal) : Convert.ToDecimal(jsonArrayDetail[j]["qty"])),
                                            ProductPrice = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["productPrice"])) ? default(decimal) : Convert.ToDecimal(jsonArrayDetail[j]["productPrice"])),
                                            Amount = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["amount"])) ? default(decimal) : Convert.ToDecimal(jsonArrayDetail[j]["amount"])),
                                            FlagPromo = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["flagPromo"])) ? default(bool) : Convert.ToBoolean(jsonArrayDetail[j]["flagPromo"])),
                                            Outstanding = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["outstanding"])) ? default(decimal) : Convert.ToDecimal(jsonArrayDetail[j]["outstanding"])),
                                            Note = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["note"])) ? default(string) : Convert.ToString(jsonArrayDetail[j]["note"])),
                                            CompaniesID = (string.IsNullOrEmpty(Convert.ToString(jsonArrayDetail[j]["companiesID"])) ? default(long) : (long)jsonArrayDetail[j]["companiesID"]),
                                        };
                                        amount += Convert.ToInt64(dataOrderDetail.Amount);
                                        dataList.Add(dataOrderDetail);
                                    }
                                    if (amount == dataOrder.Amount)
                                    {
                                        context.OrderDetails.AddRange(dataList);
                                        checkAmountSame = true;
                                    }
                                    else Trace.WriteLine($"Amount is different between order and order detail");
                                }
                                if (checkAmountSame)
                                {
                                    if (checkOrderExternalNo != null)
                                    {
                                        if (checkOrderExternalNo.OrderNo != null && checkOrderExternalNo.OrderNo != "") Trace.WriteLine($"OrderNo {checkOrderExternalNo.OrderNo} is updated");
                                        else Trace.WriteLine($"OrderNo {checkOrderExternalNo.ExternalOrderNo} is updated");
                                        context.Orders.Remove(checkOrderExternalNo);
                                    }

                                    context.Orders.Add(dataOrder);
                                    //if(cartNotZero) cartid++;
                                }
                                
                                if(paymentID != 0)
                                {
                                    Trace.WriteLine($"Sync Deposit with PaymentID {Convert.ToString(paymentID)}");

                                    var dataDeposit = new Deposit
                                    {
                                        RefID = 0,
                                        CustomersID = dataOrder.CustomersID,
                                        TotalAmount = dataOrder.AmountTotal
                                    };

                                    context.Deposits.Add(dataDeposit);

                                    Trace.WriteLine($"Sync DepositDetail with DepositID {Convert.ToString(dataDeposit.ID)}");

                                    var dataDepositDetail = new DepositDetail
                                    {
                                        RefID = 0,
                                        DepositsID = dataDeposit.ID,
                                        FromPaymentsID = paymentID,
                                        FromPaymentType = null,
                                        ToPaymentsID = 0,
                                        ToPaymentType = null,
                                        PIC = dataOrder.Username,
                                        IsReturn = false,
                                        ReturnDate = null,
                                        Amount = dataOrder.Total,
                                        BankName = null,
                                        AccountNumber = null,
                                        AccountName = null,
                                        TransferDate = null,
                                        TransferAmount = null,
                                    };
                                    context.DepositDetails.Add(dataDepositDetail);

                                    Trace.WriteLine($"Successfully Synced Deposit Data");
                                }

                                Trace.WriteLine($"Successfully Synced ExternalOrderNo {Convert.ToString(jsonArray[i]["orderNo"])}");
                                Trace.WriteLine($"----------------------------------------------------------------\n");
                            //}
                        }
                    }
                    context.SaveChanges();
                }
            } catch(Exception ex)
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

                //reguler
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

                    foreach (var row in rowsList)
                    {
                        row["orderDetail"] = context.OrderDetails
                            .Where(item => item.OrdersID == Convert.ToInt64(row["ID"]) || item.OrdersID == Convert.ToInt64(row["RefID"]))
                            .Select(item => new
                            {
                                LeadTime = context.ProductStatuses
                                    .Where(p => p.ProductID == item.ObjectID)
                                    .Select(x => x.LeadTime)
                                    .FirstOrDefault(),

                                ProductDetail = context.ProductDetails2
                                    //.Where(x => x.RefID == item.ObjectID)
                                    .Select(x => new
                                    {
                                        Type = x.Type,
                                        OriginID = x.OriginID,
                                        RefID = x.RefID,
                                        Name = x.Name,
                                        TokpedUrl = x.TokpedUrl,
                                        NewProd = x.NewProd,
                                        FavProd = x.FavProd,
                                        Image = x.Image,
                                        RealImage = x.RealImage,
                                        Weight = x.Weight,
                                        Price = x.Price,
                                        Stock = x.Stock,
                                        ClosuresID = x.ClosuresID,
                                        CategoriesID = x.CategoriesID,
                                        CategoryName = x.CategoryName,
                                        PlasticType = x.PlasticType,
                                        Functions = x.Functions,
                                        Tags = x.Tags,
                                        StockIndicator = x.StockIndicator,
                                        NecksID = x.NecksID,
                                        ColorsID = x.ColorsID,
                                        ShapesID = x.ShapesID,
                                        Volume = x.Volume,
                                        QtyPack = x.QtyPack,
                                        TotalShared = x.TotalShared,
                                        TotalViews = x.TotalViews,
                                        NewProdDate = x.NewProdDate,
                                        Height = x.Height,
                                        Length = x.Length,
                                        Width = x.Width,
                                        Diameter = x.Diameter,
                                        RimsID = x.RimsID,
                                        LidsID = x.LidsID,
                                        Status = x.Status,
                                        CountColor = x.CountColor
                                    }).FirstOrDefault(x => x.RefID == item.ObjectID && x.Type == item.ObjectType),

                                CompaniesID = item.CompaniesID,
                                Note = item.Note,
                                Outstanding = item.Outstanding,
                                FlagPromo = item.FlagPromo,
                                Amount = item.Amount,
                                ProductPrice = item.ProductPrice,
                                Qty = item.Qty,
                                QtyBox = item.QtyBox,
                                Type = item.Type,
                                PromosID = item.PromosID,
                                ObjectType = item.ObjectType,
                                ParentID = item.ParentID,
                                ObjectID = item.ObjectID,
                                OrdersID = item.OrdersID,
                                RefID = item.RefID,
                            })
                            .ToList(); // Materialize the query here
                    }

                        //reguler
                        string jsonString = JsonConvert.SerializeObject(rowsList, settings);
                        JObject jsonData = JObject.Parse("{ \"order\": " + jsonString + " }");
                        Trace.WriteLine($"data order reguler = {jsonData}");
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);

                    //reguler
                    try
                    {
                        var request = new RestRequest("/OrderReguler", Method.Post);
                        request.AddParameter("application/json", json, ParameterType.RequestBody);
                        var response = httpclient.Execute(request);

                        Trace.WriteLine($"send data reguler into web");
                        var responseContent = response.Content;

                        if (response.IsSuccessStatusCode)
                        {
                            Trace.WriteLine($"send data reguler successfully");
                            Trace.WriteLine($"content : {responseContent}");
                        }
                        else
                        {
                            Trace.WriteLine($"send data reguler failed with status code: {response.StatusCode}");
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

                    Trace.WriteLine($"Synchronization Transaction Order Reguler completed successfully.\n");
                }
                else Trace.WriteLine($"No Data.");


                //promo
                if (dataPromo != null)
                {
                    List<Dictionary<string, object>> rowsList = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dataPromo.Rows)
                    {
                        Dictionary<string, object> rowDict = new Dictionary<string, object>();
                        foreach (DataColumn column in row.Table.Columns)
                        {
                            rowDict[column.ColumnName] = row[column];
                        }
                        rowsList.Add(rowDict);
                    }

                    foreach (var row in rowsList)
                    {
                        row["orderDetail"] = context.OrderDetails
                            .Where(item => item.OrdersID == Convert.ToInt64(row["ID"]) || item.OrdersID == Convert.ToInt64(row["RefID"]))
                            .Select(item => new
                            {
                                LeadTime = context.ProductStatuses
                                    .Where(p => p.ProductID == item.ObjectID)
                                    .Select(x => x.LeadTime)
                                    .FirstOrDefault(),

                                ProductDetail = context.ProductDetails2
                                    //.Where(x => x.RefID == item.ObjectID)
                                    .Select(x => new
                                    {
                                        Type = x.Type,
                                        OriginID = x.OriginID,
                                        RefID = x.RefID,
                                        Name = x.Name,
                                        TokpedUrl = x.TokpedUrl,
                                        NewProd = x.NewProd,
                                        FavProd = x.FavProd,
                                        Image = x.Image,
                                        RealImage = x.RealImage,
                                        Weight = x.Weight,
                                        Price = x.Price,
                                        Stock = x.Stock,
                                        ClosuresID = x.ClosuresID,
                                        CategoriesID = x.CategoriesID,
                                        CategoryName = x.CategoryName,
                                        PlasticType = x.PlasticType,
                                        Functions = x.Functions,
                                        Tags = x.Tags,
                                        StockIndicator = x.StockIndicator,
                                        NecksID = x.NecksID,
                                        ColorsID = x.ColorsID,
                                        ShapesID = x.ShapesID,
                                        Volume = x.Volume,
                                        QtyPack = x.QtyPack,
                                        TotalShared = x.TotalShared,
                                        TotalViews = x.TotalViews,
                                        NewProdDate = x.NewProdDate,
                                        Height = x.Height,
                                        Length = x.Length,
                                        Width = x.Width,
                                        Diameter = x.Diameter,
                                        RimsID = x.RimsID,
                                        LidsID = x.LidsID,
                                        Status = x.Status,
                                        CountColor = x.CountColor
                                    }).FirstOrDefault(x => x.RefID == item.ObjectID && x.Type == item.ObjectType),

                                CompaniesID = item.CompaniesID,
                                Note = item.Note,
                                Outstanding = item.Outstanding,
                                FlagPromo = item.FlagPromo,
                                Amount = item.Amount,
                                ProductPrice = item.ProductPrice,
                                Qty = item.Qty,
                                QtyBox = item.QtyBox,
                                Type = item.Type,
                                PromosID = item.PromosID,
                                ObjectType = item.ObjectType,
                                ParentID = item.ParentID,
                                ObjectID = item.ObjectID,
                                OrdersID = item.OrdersID,
                                RefID = item.RefID,
                            })
                            .OrderBy(x => x.ParentID)
                            .ToList(); // Materialize the query here
                    }

                    //promo
                    string jsonStringPromo = JsonConvert.SerializeObject(rowsList, settings);
                    JObject jsonDataPromo = JObject.Parse("{ \"order\": " + jsonStringPromo + " }");
                    Trace.WriteLine($"data order promo = {jsonDataPromo}");
                    string jsonPromo = Newtonsoft.Json.JsonConvert.SerializeObject(jsonDataPromo);

                    //promo
                    try
                    {
                        var request = new RestRequest("/OrderPromo", Method.Post);
                        request.AddParameter("application/json", jsonPromo, ParameterType.RequestBody);
                        var response = httpclient.Execute(request);

                        Trace.WriteLine($"send data promo into web");
                        var responseContent = response.Content;

                        if (response.IsSuccessStatusCode)
                        {
                            Trace.WriteLine($"send data promo successfully");
                            Trace.WriteLine($"content : {responseContent}");
                        }
                        else
                        {
                            Trace.WriteLine($"send data promo failed with status code: {response.StatusCode}");
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

                    Trace.WriteLine($"Synchronization Transaction Order Promo completed successfully.\n");
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
