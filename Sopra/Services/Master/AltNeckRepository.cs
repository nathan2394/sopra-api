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
    public class AltNeckRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Neck....");
            
            var tableAltNeck = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_alt_neck WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltNeck != null)
            {
                Trace.WriteLine($"Start Sync Alt Neck {tableAltNeck.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltNeck.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltNeckID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE AltNecks WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var AltNeck = new AltNeck();

                            AltNeck.RefID = Convert.ToInt64(row["id"]);
                            AltNeck.Grouping = row["group"] == DBNull.Value ? null : row["group"].ToString();
                            AltNeckRepository.Insert(AltNeck);

                            Trace.WriteLine($"Success syncing AltNeckID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltNeckID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Neck completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_neck", "AltNecks");

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

        public static Int64 Insert(AltNeck AltNeck)
        {
            try
            {
                var sql = @"INSERT INTO [AltNecks] (Grouping, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Grouping, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                AltNeck.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltNeck);

                ///888 from integration
                //SystemLogRepository.Insert("AltNeck", AltNeck.ID, 888, AltNeck);
                return AltNeck.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(AltNeck AltNeck)
        {
            try
            {
                var sql = @"UPDATE [AltNecks] SET
                            Grouping = @Grouping,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltNeck, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltNeck", AltNeck.ID, 888, AltNeck);
                return AltNeck.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
