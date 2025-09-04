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
    public class PromoQuantityRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync PromoQuantity....");


            var tablePromoQuantity = Utility.MySqlGetObjects(string.Format(
                @"
                    select mmq.id, mmq.created_at, mmq.updated_at, 0 as promo_jumbo_id, promo_mix_id, min_quantity, 0 as price, support, level
                    from mit_promo_mix mpm
                        inner join mit_mix_quantity mmq on mpm.id  = mmq.promo_mix_id 
                    where (mpm.updated_at is null AND mpm.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mpm.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'
                ", Utility.SyncDate), Utility.MySQLDBConnection);

            if (tablePromoQuantity != null)
            {
                Trace.WriteLine($"Start Sync Promo Quantity{tablePromoQuantity.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tablePromoQuantity.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync PromoQuantityID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var condition = Convert.ToInt64(row["promo_jumbo_id"]) == 0 ? "AND PromoJumboId = 0" : "AND PromoMixId = 0";
                            Utility.ExecuteNonQuery(string.Format("DELETE PromoQuantities WHERE RefID = {0} AND IsDeleted = 0 {1}", row["ID"], condition));

                            var PromoQuantity = new PromoQuantity();

                            PromoQuantity.RefID = Convert.ToInt64(row["id"]);
                            PromoQuantity.PromoJumboId = Convert.ToInt64(row["promo_jumbo_id"]);
                            PromoQuantity.PromoMixId = Convert.ToInt64(row["promo_mix_id"]);
                            PromoQuantity.MinQuantity = Convert.ToInt32(row["min_quantity"]);
                            PromoQuantity.Price = Convert.ToDecimal(row["price"]);
                            PromoQuantity.Level = Convert.ToInt32(row["level"]);
                            PromoQuantityRepository.Insert(PromoQuantity);

                            Trace.WriteLine($"Success syncing PromoQuantityID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing PromoQuantityID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Promo Quantity completed successfully.");
                    //Utility.DeleteDiffData("mit_jumbo_quantity", "PromoQuantities", "AND PromoMixId = 0");
                    //Utility.DeleteDiffData("mit_mix_quantity", "PromoQuantities", "AND PromoJumboId = 0");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync PromoQuantity....");
        }

        public static Int64 Insert(PromoQuantity PromoQuantity)
        {
            try
            {
                var sql = @"INSERT INTO [PromoQuantities] (RefID ,PromoJumboId ,PromoMixId ,MinQuantity,Price,Level ,DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID ,@PromoJumboId ,@PromoMixId ,@MinQuantity,@Price,@Level,@DateIn, @UserIn, @IsDeleted) ";
                PromoQuantity.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, PromoQuantity);

                ///888 from integration
                //SystemLogRepository.Insert("PromoQuantity", PromoQuantity.ID, 888, PromoQuantity);
                return PromoQuantity.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(PromoQuantity PromoQuantity)
        {
            try
            {
                var sql = @"UPDATE [PromoQuantities] SET
                            PromoJumboId = @PromoJumboId ,
                            PromoMixId = @PromoMixId ,
                            MinQuantity = @MinQuantity,
                            Price = @Price,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, PromoQuantity, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("PromoQuantity", PromoQuantity.ID, 888, PromoQuantity);
                return PromoQuantity.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
