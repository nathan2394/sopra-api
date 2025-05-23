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
    public class AltWeightRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Weight Bottle....");
            

            var tableAltWeightBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_alt_weight WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltWeightBottle != null)
            {
                Trace.WriteLine($"Start Sync Alt Weight Bottle {tableAltWeightBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltWeightBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltWeightID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE AltWeights WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Bottle'", row["ID"]));

                            var AltWeight = new AltWeight();

                            AltWeight.RefID = Convert.ToInt64(row["id"]);
                            AltWeight.Grouping = row["group"] == DBNull.Value ? null : row["group"].ToString();
                            AltWeight.Type = "Bottle";
                            AltWeightRepository.Insert(AltWeight);

                            Trace.WriteLine($"Success syncing AltWeightID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltWeightID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Weight Bottle completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_weight", "AltWeights", "AND Type = 'Bottle'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //GET DATA FROM MYSQL
            //var tableAltWeight = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_alt_weight_thermo WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            var tableAltWeightThermo = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_alt_weight_thermo WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltWeightThermo != null)
            {
                Trace.WriteLine($"Start Sync Alt Weight Thermo {tableAltWeightThermo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltWeightThermo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltWeightID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE AltWeights WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Thermo'", row["ID"]));

                            var AltWeight = new AltWeight();

                            AltWeight.RefID = Convert.ToInt64(row["id"]);
                            AltWeight.Grouping = row["group"] == DBNull.Value ? null : row["group"].ToString();
                            AltWeight.Type = "Thermo";
                            AltWeightRepository.Insert(AltWeight);

                            Trace.WriteLine($"Success syncing AltWeightID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltWeightID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Weight Thermo completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_weight_thermo", "AltWeights", "AND Type = 'Thermo'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Alt Weight....");
        }

        public static Int64 Insert(AltWeight AltWeight)
        {
            try
            {
                var sql = @"INSERT INTO [AltWeights] (Grouping, RefID, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Grouping, @RefID, @Type, @DateIn, @UserIn, @IsDeleted) ";
                AltWeight.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltWeight);

                ///888 from integration
                //SystemLogRepository.Insert("AltWeight", AltWeight.ID, 888, AltWeight);
                return AltWeight.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(AltWeight AltWeight)
        {
            try
            {
                var sql = @"UPDATE [AltWeights] SET
                            Group = @Group,
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltWeight, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltWeight", AltWeight.ID, 888, AltWeight);
                return AltWeight.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
