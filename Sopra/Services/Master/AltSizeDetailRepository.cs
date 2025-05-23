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

namespace Sopra.Services.Master
{
    public class AltSizeDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Size Detail....");
            
            var tableAltSizeDetail = Utility.MySqlGetObjects(string.Format("select * from mit_alt_size_detail WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltSizeDetail != null)
            {
                Trace.WriteLine($"Start Sync Alt Size Detail {tableAltSizeDetail.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltSizeDetail.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltSizeDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE AltSizeDetails WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));
                            var AltSizeDetail = new AltSizeDetail();

                            AltSizeDetail.RefID = Convert.ToInt64(row["id"]);
                            AltSizeDetail.AltSizeID = row["alt_size_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["alt_size_id"]);
                            AltSizeDetail.ThermosID = row["mit_thermos_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["mit_thermos_id"]);
                            AltSizeDetailRepository.Insert(AltSizeDetail);

                            Trace.WriteLine($"Success syncing AltSizeDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltSizeDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Size Detail completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_size_detail", "AltSizeDetails");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Alt Size Detail....");
        }

        public static Int64 Insert(AltSizeDetail AltSizeDetail)
        {
            try
            {
                var sql = @"INSERT INTO [AltSizeDetails] (ThermosID,AltSizeID, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@ThermosID,@AltSizeID ,@RefID, @DateIn, @UserIn, @IsDeleted) ";
                AltSizeDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltSizeDetail);

                ///888 from integration
                //SystemLogRepository.Insert("AltSizeDetail", AltSizeDetail.ID, 888, AltSizeDetail);
                return AltSizeDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(AltSizeDetail AltSizeDetail)
        {
            try
            {
                var sql = @"UPDATE [AltSizeDetails] SET
                            ThermosID = @ThermosID,
                            AltSizeID = @AltSizeID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltSizeDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltSizeDetail", AltSizeDetail.ID, 888, AltSizeDetail);
                return AltSizeDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
