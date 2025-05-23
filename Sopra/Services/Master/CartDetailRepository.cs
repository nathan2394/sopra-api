using System;
using System.Diagnostics;
//using System.Linq;
using System.Threading;
using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;

using Sopra.Helpers;
using Sopra.Entities;
namespace Sopra.Services
{
    public class CartDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Cart Detail....");
            

            var tableCartDetailBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_carts_detail_bottle WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCartDetailBottle != null)
            {
                Trace.WriteLine($"Start Sync Cart Detail Bottle {tableCartDetailBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCartDetailBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CartDetailID Bottle : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE CartDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'bottle'", row["ID"]));

                            var CartDetail = new CartDetail();

                            CartDetail.RefID = Convert.ToInt64(row["id"]);
                            CartDetail.CartsID = row["carts_id"] == DBNull.Value ? 0 :  Convert.ToInt64(row["carts_id"]);
                            CartDetail.ObjectID = row["products_id"] == DBNull.Value ? 0 :  Convert.ToInt64(row["products_id"]);
                            CartDetail.Type = "bottle";
                            CartDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["qty_box"]);
                            CartDetail.Qty = row["qty"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["qty"]);
                            CartDetail.Price = row["product_price"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["product_price"]);
                            CartDetail.Amount = row["amount"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["amount"]);
                            CartDetail.IsCheckout = row["is_checkout"] == DBNull.Value ? false : Convert.ToBoolean(row["is_checkout"]);
                            CartDetailRepository.Insert(CartDetail);

                            Trace.WriteLine($"Success syncing CartDetailID Bottle : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CartDetailID Bottle: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Cart Detail Bottle completed successfully.");
                    //Utility.DeleteDiffData("mit_carts_detail_bottle", "CartDetails", "AND Type = 'bottle'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            var tableCartDetailThermo = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_carts_detail_thermo WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCartDetailThermo != null)
            {
                Trace.WriteLine($"Start Sync Cart Detail Thermo {tableCartDetailThermo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCartDetailThermo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CartDetailID Thermo : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE CartDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'thermo'", row["ID"]));

                            var CartDetail = new CartDetail();

                            CartDetail.RefID = Convert.ToInt64(row["id"]);
                            CartDetail.CartsID = row["carts_id"] == DBNull.Value ? 0 :  Convert.ToInt64(row["carts_id"]);
                            CartDetail.ObjectID = row["products_id"] == DBNull.Value ? 0 :  Convert.ToInt64(row["products_id"]);
                            CartDetail.Type = "thermo";
                            CartDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["qty_box"]);
                            CartDetail.Qty = row["qty"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["qty"]);
                            CartDetail.Price = row["product_price"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["product_price"]);
                            CartDetail.Amount = row["amount"] == DBNull.Value ? 0 :  Convert.ToDecimal(row["amount"]);
                            CartDetail.IsCheckout = row["is_checkout"] == DBNull.Value ? false : Convert.ToBoolean(row["is_checkout"]);
                            CartDetailRepository.Insert(CartDetail);

                            Trace.WriteLine($"Success syncing CartDetailID Thermo : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CartDetailID Thermo : {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Cart Detail Thermo completed successfully.");
                    //Utility.DeleteDiffData("mit_carts_detail_thermo", "CartDetails", "AND Type = 'thermo'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Cart Detail....");
        }

        public static Int64 Insert(CartDetail CartDetail)
        {
            try
            {
                var sql = @"INSERT INTO [CartDetails] (RefID, CartsID, ObjectID, Type, QtyBox, Qty, Price, Amount, IsCheckout, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @CartsID, @ObjectID, @Type, @QtyBox, @Qty, @Price, @Amount, @IsCheckout, @DateIn, @UserIn, @IsDeleted) ";
                CartDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, CartDetail);

                ///888 from integration
                //SystemLogRepository.Insert("CartDetail", CartDetail.ID, 888, CartDetail);
                return CartDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(CartDetail CartDetail)
        {
            try
            {
                var sql = @"UPDATE [CartDetails] SET
                            CartsID = @CartsID,
                            ObjectID = @ObjectID,
                            Type = @Type,
                            QtyBox = @QtyBox,
                            Qty = @Qty,
                            Price = @Price,
                            Amount = @Amount,
                            IsCheckout = @IsCheckout,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, CartDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("CartDetail", CartDetail.ID, 888, CartDetail);
                return CartDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
