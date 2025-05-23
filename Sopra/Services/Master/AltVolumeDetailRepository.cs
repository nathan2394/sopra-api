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
    public class AltVolumeDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Volume Detail....");

            var tableAltVolumeDetail = Utility.MySqlGetObjects(string.Format("SELECT mavd.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_volume_detail mavd LEFT JOIN mit_products ON mavd.products_id = mit_products.id WHERE (mavd.updated_at is null AND mavd.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mavd.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltVolumeDetail != null)
            {
                Trace.WriteLine($"Start Sync Alt Volume Detail {tableAltVolumeDetail.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltVolumeDetail.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltVolumeDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE AltVolumeDetails WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var AltVolumeDetail = new AltVolumeDetail();

                            AltVolumeDetail.RefID = Convert.ToInt64(row["id"]);
                            AltVolumeDetail.AltVolumesID = row["alt_volume_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["alt_volume_id"]);
                            AltVolumeDetail.CalculatorsID = row["calculatorid"] == DBNull.Value ? 0 : Convert.ToInt64(row["calculatorid"]);
                            AltVolumeDetailRepository.Insert(AltVolumeDetail);

                            Trace.WriteLine($"Success syncing AltVolumeDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltVolumeDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Volume completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_volume_detail", "AltVolumeDetails");

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

        public static Int64 Insert(AltVolumeDetail AltVolumeDetail)
        {
            try
            {
                var sql = @"INSERT INTO [AltVolumeDetails] (AltVolumesID, CalculatorsID, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@AltVolumesID, @CalculatorsID, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                AltVolumeDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltVolumeDetail);

                ///888 from integration
                //SystemLogRepository.Insert("AltVolumeDetail", AltVolumeDetail.ID, 888, AltVolumeDetail);
                return AltVolumeDetail.ID;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error inserting AltVolumeDetail: {AltVolumeDetail.AltVolumesID} - {ex.Message}");
                throw;
            }
        }

        public static Int64 Update(AltVolumeDetail AltVolumeDetail)
        {
            try
            {
                var sql = @"UPDATE [AltVolumeDetails] SET
                            AltVolumesID = @AltVolumesID,
                            CalculatorsID = @CalculatorsID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltVolumeDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltVolumeDetail", AltVolumeDetail.ID, 888, AltVolumeDetail);
                return AltVolumeDetail.ID;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error updating AltVolumeDetail: {AltVolumeDetail.AltVolumesID} - {ex.Message}");
                throw;
            }
        }
    }
}
