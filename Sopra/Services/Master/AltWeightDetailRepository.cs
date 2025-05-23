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
    public class AltWeightDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Weight Detail Bottle....");
            

            var tableAltWeightDetailBottle = Utility.MySqlGetObjects(string.Format("SELECT mawd.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_weight_detail mawd LEFT JOIN mit_products ON mawd.products_id = mit_products.id WHERE (mawd.updated_at is null AND mawd.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mawd.updated_at > '{0:yyyy-MM-dd HH:mm:ss}' ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltWeightDetailBottle != null)
            {
                Trace.WriteLine($"Start Sync Alt Weight Detail Bottle {tableAltWeightDetailBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltWeightDetailBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltWeightDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE AltWeightDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Bottle'", row["ID"]));

                            var AltWeightDetail = new AltWeightDetail();

                            AltWeightDetail.RefID = Convert.ToInt64(row["id"]);
                            AltWeightDetail.AltWeightsID = row["alt_weight_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["alt_weight_id"]);
                            AltWeightDetail.ObjectID = row["calculatorid"] == DBNull.Value ? 0 : Convert.ToInt64(row["calculatorid"]);
                            AltWeightDetail.Type = "Bottle";
                            AltWeightDetailRepository.Insert(AltWeightDetail);

                            Trace.WriteLine($"Success syncing AltWeightDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltWeightDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Weight Detail Bottle completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_weight_detail", "AltWeightDetails", "Type = 'Bottle'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //var tableAltWeightDetailThermo = Utility.MySqlGetObjects(string.Format("SELECT mit_alt_weight_detail.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_volume_detail LEFT JOIN mit_products ON mit_alt_volume_detail.products_id = mit_products.id WHERE (mit_alt_volume_detail.updated_at is null AND mit_alt_volume_detail.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mit_alt_volume_detail.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            var tableAltWeightDetailThermo = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_alt_weight_thermo_detail WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltWeightDetailThermo != null)
            {
                Trace.WriteLine($"Start Sync Alt Weight Detail Thermo {tableAltWeightDetailThermo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltWeightDetailThermo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltWeightDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            
                            Utility.ExecuteNonQuery(string.Format("DELETE AltWeightDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Thermo'", row["ID"]));

                            var AltWeightDetail = new AltWeightDetail();

                            AltWeightDetail.RefID = Convert.ToInt64(row["id"]);
                            AltWeightDetail.AltWeightsID = row["alt_weight_thermo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["alt_weight_thermo_id"]);
                            AltWeightDetail.ObjectID = row["mit_thermos_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_thermos_id"]);
                            AltWeightDetail.Type = "Thermo";
                            AltWeightDetailRepository.Insert(AltWeightDetail);


                            Trace.WriteLine($"Success syncing AltWeightDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltWeightDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Weight Detail Thermo completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_weight_thermo_detail", "AltWeightDetails", "AND Type = 'Thermo'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
            Trace.WriteLine("Finished Sync Alt Weight Detail....");
        }

        public static Int64 Insert(AltWeightDetail AltWeightDetail)
        {
            try
            {
                var sql = @"INSERT INTO [AltWeightDetails] (AltWeightsID, ObjectID, RefID, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@AltWeightsID, @ObjectID, @RefID, @Type, @DateIn, @UserIn, @IsDeleted) ";
                AltWeightDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltWeightDetail);

                ///888 from integration
                //SystemLogRepository.Insert("AltWeightDetail", AltWeightDetail.ID, 888, AltWeightDetail);
                return AltWeightDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(AltWeightDetail AltWeightDetail)
        {
            try
            {
                var sql = @"UPDATE [AltWeightDetails] SET
                            AltWeightsID = @AltWeightsID,
                            ObjectID = @ObjectID,
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltWeightDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltWeightDetail", AltWeightDetail.ID, 888, AltWeightDetail);
                return AltWeightDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
