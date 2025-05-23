using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Linq;
using System.Text;
using System.Threading;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using Sopra.Helpers;
using Sopra.Entities;

namespace Sopra.Services
{
    public class ProductStatusRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync ProductStatus....");

            //MYSQL
            Utility.ExecuteNonQueryMySQL($"truncate table v_product_Status");
            Utility.ExecuteNonQueryMySQL($"insert into v_product_status select * from v_product_status_2");

            //UPDATE OS v_product_status di MYSQL
            //LOOPING OS

            Utility.ExecuteNonQuery(string.Format("TRUNCATE TABLE ProductStatus"));

            //SQL
            var tableProductStatus = Utility.MySqlGetObjects(string.Format("select * from v_product_status_2", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableProductStatus != null)
            {
                Trace.WriteLine($"Start Sync ProductStatus {tableProductStatus.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableProductStatus.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ProductStatusID : {Convert.ToInt64(row["product_id"])}");

                            var ProductStatus = new ProductStatus();
                            ProductStatus.ProductID = row["product_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_id"]);
                            ProductStatus.WmsCode = row["wms_code"] == DBNull.Value ? string.Empty : Convert.ToString(row["wms_code"]);
                            ProductStatus.ProductName = row["product_name"] == DBNull.Value ? string.Empty : Convert.ToString(row["product_name"]);
                            ProductStatus.TotalQty = row["total_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_qty"]);
                            ProductStatus.ShippingQty = row["shipping_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["shipping_qty"]);
                            ProductStatus.Outstanding = row["outstanding"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstanding"]);
                            ProductStatus.DataStock = row["data_stock"] == DBNull.Value ? 0 : Convert.ToInt32(row["data_stock"]);
                            ProductStatus.DailyOutput = row["daily_output"] == DBNull.Value ? 0 : Convert.ToDecimal(row["daily_output"]);
                            ProductStatus.StockAvail = row["stock_avail"] == DBNull.Value ? 0 : Convert.ToDecimal(row["stock_avail"]);
                            ProductStatus.StockStatus = row["stock_status"] == DBNull.Value ? string.Empty : Convert.ToString(row["stock_status"]);
                            ProductStatus.CountOrder = row["count_order"] == DBNull.Value ? 0 : Convert.ToInt64(row["count_order"]);
                            ProductStatus.WishList = row["wishlist"] == DBNull.Value ? 0 : Convert.ToInt64(row["wishlist"]);
                            ProductStatus.TotalShared = row["total_shared"] == DBNull.Value ? 0 : Convert.ToInt32(row["total_shared"]);
                            ProductStatus.TotalViews = row["total_views"] == DBNull.Value ? 0 : Convert.ToInt32(row["total_views"]);
                            ProductStatus.Score = row["score"] == DBNull.Value ? 0 : Convert.ToInt64(row["score"]);
                            ProductStatus.PrepTime = row["prep_time"] == DBNull.Value ? 0 : Convert.ToInt64(row["prep_time"]);
                            ProductStatus.ProdTime = row["prod_time"] == DBNull.Value ? 0 : Convert.ToDecimal(row["prod_time"]);
                            ProductStatus.LeadTime = row["lead_time"] == DBNull.Value ? 0 : Convert.ToDecimal(row["lead_time"]);
                            ProductStatusRepository.Insert(ProductStatus);

                            Trace.WriteLine($"Success syncing ProductStatusID : {Convert.ToInt64(row["product_id"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ProductStatusID: {Convert.ToInt64(row["product_id"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization ProductStatus completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync ProductStatus....");
        }

        public static Int64 Insert(ProductStatus ProductStatus)
        {
            try
            {
                var sql =
                        @"
                            INSERT INTO [ProductStatus] (
                                ProductID, WmsCode, ProductName, TotalQty, ShippingQty, Outstanding, DataStock, DailyOutput, StockAvail, StockStatus, CountOrder, WishList, TotalShared, TotalViews, Score, PrepTime, ProdTime, LeadTime
                            ) 
                            VALUES (
                                @ProductID, @WmsCode, @ProductName, @TotalQty, @ShippingQty, @Outstanding, @DataStock, @DailyOutput, @StockAvail, @StockStatus, @CountOrder, @WishList, @TotalShared, @TotalViews, @Score, @PrepTime, @ProdTime, @LeadTime
                            ) 
                        ";
                Utility.SQLDBConnection.ExecuteScalar(sql, ProductStatus);

                ///888 from integration
                Trace.WriteLine($"Inserted ProductStatus : {ProductStatus.WmsCode}");
                return (long)ProductStatus.ProductID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
