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
using System.Globalization;

namespace Sopra.Services
{
    public class PromoRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Promo....");

            var tablePromo = Utility.MySqlGetObjects(string.Format(
                @"
                    select *,'Mix' as type 
                    from mit_promo_mix 
                    WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'
                ", Utility.SyncDate), Utility.MySQLDBConnection);

            if (tablePromo != null)
            {
                Trace.WriteLine($"Start Sync Promo {tablePromo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tablePromo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync PromoID : {Convert.ToInt64(row["id"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var type = row["type"].ToString();
                            Utility.ExecuteNonQuery(string.Format("DELETE Promos WHERE RefID = {0} AND IsDeleted = 0 AND Type = '{1}'", row["ID"], type));

                            var Promo = new Promo();

                            Promo.RefID = Convert.ToInt64(row["id"]);
                            Promo.Name = row["name"].ToString();
                            Promo.PromoDesc = row["promo_desc"].ToString();
                            Promo.StartDate = row["start_date"] == DBNull.Value ? (DateTime?)null : DateTime.ParseExact(row["start_date"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            Promo.EndDate = row["end_date"] == DBNull.Value ? (DateTime?)null : DateTime.ParseExact(row["end_date"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture).Date.AddDays(1).AddSeconds(-1);
                            Promo.Image = row["image"] == DBNull.Value ? null : row["image"].ToString();
                            Promo.ImgThumb = row["img_thumb"] == DBNull.Value ? null : row["img_thumb"].ToString();
                            Promo.Category = row["category"] == DBNull.Value ? null : Convert.ToInt32(row["category"]);
                            Promo.Type = type;
                            PromoRepository.Insert(Promo);

                            Trace.WriteLine($"Success syncing PromoID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing PromoID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Promo completed successfully.");

                    //Utility.DeleteDiffData("mit_promo_mix", "Promos", "AND Type = 'Mix'");
                    //Utility.DeleteDiffData("mit_promo_jumbo", "Promos", "AND Type = 'Jumbo'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Promo....");
        }

        public static Int64 Insert(Promo Promo)
        {
            try
            {
                var sql = @"INSERT INTO [Promos] (RefID,Name,PromoDesc,StartDate,EndDate,Image,ImgThumb,Category,Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID,@Name,@PromoDesc,@StartDate,@EndDate,@Image,@ImgThumb,@Category,@Type,@DateIn, @UserIn, @IsDeleted) ";
                Promo.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Promo);

                ///888 from integration
                //SystemLogRepository.Insert("Promo", Promo.ID, 888, Promo);
                return Promo.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Promo Promo)
        {
            try
            {
                var sql = @"UPDATE [Promos] SET
                            Name = @Name,
                            PromoDesc = @PromoDesc,
                            StartDate = @StartDate,
                            EndDate = @EndDate,
                            Image = @Image ,
                            ImgThumb = @ImgThumb,
                            Category = @Category,
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Promo, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Promo", Promo.ID, 888, Promo);
                return Promo.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
