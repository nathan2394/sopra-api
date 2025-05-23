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
    public class AltVolumeRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Volume....");

            var tableAltVolume = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_alt_volume WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltVolume != null)
            {
                Trace.WriteLine($"Start Sync Alt Volume {tableAltVolume.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltVolume.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltVolumeID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE AltVolumes WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var AltVolume = new AltVolume();

                            AltVolume.RefID = Convert.ToInt64(row["id"]);
                            AltVolume.Grouping = row["group"] == DBNull.Value ? null : row["group"].ToString();
                            AltVolumeRepository.Insert(AltVolume);
                            Trace.WriteLine($"Success syncing AltVolumeID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltVolumeID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Volume completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_volume", "AltVolumes");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Alt Volume....");
        }

        public static Int64 Insert(AltVolume AltVolume)
        {
            try
            {
                var sql = @"INSERT INTO [AltVolumes] (Grouping, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Grouping, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                AltVolume.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltVolume);

                ///888 from integration
                //SystemLogRepository.Insert("AltVolume", AltVolume.ID, 888, AltVolume);
                return AltVolume.ID;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error during AltVolume Insert: {ex.Message}");
                throw;
            }
        }

        public static Int64 Update(AltVolume AltVolume)
        {
            try
            {
                var sql = @"UPDATE [AltVolumes] SET
                            Grouping = @Grouping,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltVolume, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltVolume", AltVolume.ID, 888, AltVolume);
                return AltVolume.ID;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error during AltVolume Update: {ex.Message}");
                throw;
            }
        }
    }
}
