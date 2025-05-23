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
    public class FavoriteRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Favorite....");
           

            var tableFavorite = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_favorites WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableFavorite != null)
            {
                Trace.WriteLine($"Start Sync Favorite {tableFavorite.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableFavorite.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync FavoriteID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Favorites WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));


                            var Favorite = new Favorite();

                            Favorite.RefID = Convert.ToInt64(row["id"]);
                            Favorite.ObjectID = row["mit_products_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_products_id"]);
                            Favorite.Type = row["type"] == DBNull.Value ? null : row["type"].ToString();
                            Favorite.Accs1ID = row["accs1_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["accs1_id"]);
                            Favorite.Accs2ID = row["accs2_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["accs2_id"]);
                            Favorite.SalesPrice = row["sales_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["sales_price"]);
                            Favorite.PriceAcc1 = row["price_acc1"] == DBNull.Value ? 0 : Convert.ToDecimal(row["price_acc1"]);
                            Favorite.PriceAcc2 = row["price_acc2"] == DBNull.Value ? 0 : Convert.ToDecimal(row["price_acc2"]);
                            Favorite.UsersID = row["mit_users_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_users_id"]);
                            FavoriteRepository.Insert(Favorite);

                            Trace.WriteLine($"Success syncing FavoriteID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing FavoriteID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Neck completed successfully.");
                    //Utility.DeleteDiffData("mit_favorites", "Favorites");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Alt Neck....");
        }

        public static Int64 Insert(Favorite Favorite)
        {
            try
            {
                var sql = @"INSERT INTO [Favorites] (RefID, ObjectID, Type, Accs1ID, Accs2ID, SalesPrice, PriceAcc1, PriceAcc2, UsersID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @ObjectID, @Type, @Accs1ID, @Accs2ID, @SalesPrice, @PriceAcc1, @PriceAcc2, @UsersID, @DateIn, @UserIn, @IsDeleted) ";
                Favorite.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Favorite);

                ///888 from integration
                //SystemLogRepository.Insert("Favorite", Favorite.ID, 888, Favorite);
                return Favorite.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Favorite Favorite)
        {
            try
            {
                var sql = @"UPDATE [Favorites] SET
                            ObjectID = @ObjectID,
                            Type = @Type,
                            Accs1ID = @Accs1ID,
                            Accs2ID = @Accs2ID,
                            SalesPrice = @SalesPrice,
                            PriceAcc1 = @PriceAcc1,
                            PriceAcc2 = @PriceAcc2,
                            UsersID = @UsersID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Favorite, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Favorite", Favorite.ID, 888, Favorite);
                return Favorite.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
