using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;
using RestSharp;
using Google.Api;
using Newtonsoft.Json.Linq;

namespace SOPRA.Services
{
    public class TransactionOrderDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Transaction Order Detail....");
            //define restClient
            var url = Utility.APIURL;
            var httpclient = new RestClient(url);

            try
            {
                var request = new RestRequest("/getBestSellerProductName", Method.Get);
                request.AddHeader("Content-Type", "application/json");
                var response = httpclient.Execute(request);
                if (response.IsSuccessStatusCode)
                {
                    // Parse the JSON string
                    var json = JArray.Parse(response.Content);

                    // Access the "order" array inside the "response"
                    JArray jsonArray = json;
                    if (jsonArray != null && jsonArray.Count > 0)
                    {
                        for (int i = 0; i < jsonArray.Count; i++)
                        {
                            var objectId = string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["id"])) ? default(long) : Convert.ToInt64(jsonArray[i]["id"]);
                            
                            Utility.ExecuteNonQuery(string.Format("DELETE BestSellers WHERE ObjectID = {0} AND IsDeleted = 0", objectId));
                            var bestSeller = new BestSeller();

                            bestSeller.ObjectID = objectId;
                            bestSeller.Type = string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["type"])) ? default(string) : Convert.ToString(jsonArray[i]["type"]);
                            bestSeller.Qty = string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["total"])) ? default(decimal) : Convert.ToDecimal(jsonArray[i]["total"]);
                            bestSeller.ProductName = string.IsNullOrEmpty(Convert.ToString(jsonArray[i]["name"])) ? default(string) : Convert.ToString(jsonArray[i]["name"]);
                            TransactionOrderDetailRepository.Insert(bestSeller);
                            
                            Trace.WriteLine($"Success syncing BestSellers with ObjectID : {objectId}");
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error during synchronization: {ex.Message}");
            }

            //where (v_transaction_order_detail.order_date is null AND v_transaction_order_detail.order_date > '{0:yyyy-MM-dd HH:mm:ss}') OR v_transaction_order_detail.order_date > '{0:yyyy-MM-dd HH:mm:ss}'
            //var tableTransactionOrderDetails = Utility.MySqlGetObjects(string.Format("select vtod.*,mit_products.mit_calculators_id as objectid from v_transaction_order_detail vtod join mit_products on vtod.product_id = mit_products.id  where vtod.order_date >= '2024-01-01' and vtod.order_status = 'ACTIVE'", Utility.SyncDate), Utility.MySQLDBConnection);
            //if (tableTransactionOrderDetails != null)
            //{
            //    Trace.WriteLine($"Start Sync TransactionOrderDetail {tableTransactionOrderDetails.Rows.Count} Data(s)....");
            //    try
            //    {
            //        ///LOOPING DATA
            //        foreach (DataRow row in tableTransactionOrderDetails.Rows)
            //        {
            //            try
            //            {
            //                Trace.WriteLine($"Sync TransactionOrderDetailID : {Convert.ToInt64(row["order_id"])}");
            //                ///CHECK DATA EXISTS / TIDAK DI SQL
            //                //Utility.ExecuteNonQuery(string.Format("DELETE TransactionOrderDetails WHERE RefID = {0} AND IsDeleted = 0", row["order_id"]));

            //                var TransactionOrderDetail = new TransactionOrderDetail();

            //                TransactionOrderDetail.RefID = row["order_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["order_id"]);
            //                TransactionOrderDetail.CompanyID = row["company_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["company_id"]);
            //                TransactionOrderDetail.OrdersID = row["order_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["order_id"]);
            //                TransactionOrderDetail.OrderNo = row["order_no"] == DBNull.Value ? null : row["order_no"].ToString();
            //                TransactionOrderDetail.OrderDate = row["order_date"] == DBNull.Value ? default(DateTime) : Convert.ToDateTime(row["order_date"]);
            //                TransactionOrderDetail.CustomerName = row["customer_name"] == DBNull.Value ? null : row["customer_name"].ToString();
            //                TransactionOrderDetail.CustomersID = row["customer_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["customer_id"]);
            //                TransactionOrderDetail.ProvinceName = row["province_name"] == DBNull.Value ? null : row["province_name"].ToString();
            //                TransactionOrderDetail.RegencyName = row["regency_name"] == DBNull.Value ? null : row["regency_name"].ToString();
            //                TransactionOrderDetail.DistrictName = row["district_name"] == DBNull.Value ? null : row["district_name"].ToString();
            //                TransactionOrderDetail.ObjectID = row["objectid"] == DBNull.Value ? 0 : Convert.ToInt64(row["objectid"]);
            //                TransactionOrderDetail.ProductName = row["product_name"] == DBNull.Value ? null : row["product_name"].ToString();
            //                TransactionOrderDetail.ProductType = "bottle";
            //                TransactionOrderDetail.Qty = row["qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty"]);
            //                TransactionOrderDetail.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
            //                TransactionOrderDetail.Type = row["type"] == DBNull.Value ? 0 : Convert.ToInt64(row["type"]);
            //                TransactionOrderDetail.InvoicesID = row["invoice_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["invoice_id"]);
            //                TransactionOrderDetail.InvoiceNo = row["invoice_no"] == DBNull.Value ? null : row["invoice_no"].ToString();
            //                TransactionOrderDetail.PaymentsID = row["payment_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["payment_id"]);
            //                TransactionOrderDetail.PaymentsNo = row["payment_no"] == DBNull.Value ? null : row["payment_no"].ToString();
            //                TransactionOrderDetail.Status = row["status"] == DBNull.Value ? null : row["status"].ToString();
            //                TransactionOrderDetail.LinkedWMS = row["linked_wms"] == DBNull.Value ? 0 : Convert.ToInt64(row["linked_wms"]);
            //                TransactionOrderDetail.DealerTier = row["linked_wms"] == DBNull.Value ? null : row["linked_wms"].ToString();
            //                TransactionOrderDetail.OrderStatus = row["order_status"] == DBNull.Value ? null : row["order_status"].ToString();
            //                TransactionOrderDetailRepository.Insert(TransactionOrderDetail);


            //                Trace.WriteLine($"Success syncing TransactionOrderDetailID : {Convert.ToInt64(row["order_id"])}");
            //                Thread.Sleep(100);
            //            }
            //            catch (Exception ex)
            //            {
            //                Trace.WriteLine($"Error syncing TransactionOrderDetailID: {Convert.ToInt64(row["order_id"])} - {ex.Message}");
            //                Thread.Sleep(100);
            //            }
            //        }

            //        Trace.WriteLine($"Synchronization TransactionOrderDetail completed successfully.");
            //        //Utility.DeleteDiffData("v_transaction_order_detail", "TransactionOrderDetails");

            //        Trace.WriteLine($"Delete Diff Data completed successfully.");
            //    }
            //    catch (Exception ex)
            //    {
            //        Trace.WriteLine($"Error during synchronization: {ex.Message}");
            //    }
            //}

            Trace.WriteLine("Finished Sync Transaction Order Detail....");
        }

        public static Int64 Insert(BestSeller BestSeller)
        {
            try
            {
                var sql = @"INSERT INTO [BestSellers] (RefID, ProductName, ObjectID, Type, Qty, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @ProductName, @ObjectID, @Type, @Qty, @DateIn, @UserIn, @IsDeleted) ";
                BestSeller.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, BestSeller);

                ///888 from integration
                //SystemLogRepository.Insert("TransactionOrderDetail", TransactionOrderDetail.ID, 888, TransactionOrderDetail);
                return BestSeller.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(TransactionOrderDetail TransactionOrderDetail)
        {
            try
            {
                var sql = @"UPDATE [TransactionOrderDetails] SET
                            CompanyID = @CompanyID,
                            OrdersID = @OrdersID,
                            OrderNo = @OrderNo,
                            OrderDate = @OrderDate,
                            CustomerName = @CustomerName,
                            CustomersID = @CustomersID,
                            ProvinceName = @ProvinceName,
                            RegencyName = @RegencyName,
                            DistrictName = @DistrictName,
                            ObjectID = @ObjectID,
                            ProductName = @ProductName,
                            ProductType = @ProductType,
                            Qty = @Qty,
                            Amount = @Amount,
                            Type = @Type,
                            InvoicesID = @InvoicesID,
                            InvoiceNo = @InvoiceNo,
                            PaymentsID = @PaymentsID,
                            PaymentsNo = @PaymentsNo,
                            Status = @Status,
                            LinkedWMS = @LinkedWMS,
                            DealerTier = @DealerTier,
                            OrderStatus = @OrderStatus,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, TransactionOrderDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("TransactionOrderDetail", TransactionOrderDetail.ID, 888, TransactionOrderDetail);
                return TransactionOrderDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
