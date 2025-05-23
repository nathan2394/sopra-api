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
    public class CartRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Cart....");
            //var deleteData = Utility.SQLDBConnection.QueryFirstOrDefault<Cart>(string.Format("TRUNCATE TABLE Carts"), transaction: null);
            //GET DATA FROM MYSQL
            var tableCart = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_carts WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCart != null)
            {
                Trace.WriteLine($"Start Sync Cart {tableCart.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCart.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CartID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Carts WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Cart = new Cart();

                            Cart.RefID = Convert.ToInt64(row["id"]);
                            Cart.CustomersID = row["customers_id"] == DBNull.Value ? 0 :  Convert.ToInt64(row["customers_id"]);
                            Cart.PromosID = row["mit_new_promo_bottle_id"] == DBNull.Value ? 0 :  Convert.ToInt64(row["mit_new_promo_bottle_id"]);
                            Cart.Status = row["status"] == DBNull.Value ? 0 :  Convert.ToInt64(row["status"]);
                            CartRepository.Insert(Cart);

                            Trace.WriteLine($"Success syncing CartID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CartID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Cart completed successfully.");
                    //Utility.DeleteDiffData("mit_carts", "Carts");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Cart....");
        }

        public static Int64 Insert(Cart Cart)
        {
            try
            {
                var sql = @"INSERT INTO [Carts] (RefID, CustomersID, PromosID, Status, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @CustomersID, @PromosID, @Status, @DateIn, @UserIn, @IsDeleted) ";
                Cart.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Cart);

                ///888 from integration
                //SystemLogRepository.Insert("Cart", Cart.ID, 888, Cart);
                return Cart.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Cart Cart)
        {
            try
            {
                var sql = @"UPDATE [Carts] SET
                            CustomersID = @CustomersID,
                            PromosID = @PromosID,
                            Status = @Status,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Cart, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Cart", Cart.ID, 888, Cart);
                return Cart.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
