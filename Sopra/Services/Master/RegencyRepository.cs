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
    public class RegencyRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Regency....");
            

            var tableRegency = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_regencies ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableRegency != null)
            {
                Trace.WriteLine($"Start Sync Regency {tableRegency.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableRegency.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync RegencyID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Regencies WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Regency = new Regency();

                            Regency.RefID = Convert.ToInt64(row["id"]);
                            Regency.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            Regency.ProvincesID = row["province_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["province_id"]);
                            RegencyRepository.Insert(Regency);

                            Trace.WriteLine($"Success syncing RegencyID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing RegencyID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Regency completed successfully.");
                    //Utility.DeleteDiffData("mit_regencies", "Regencies");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Regency....");
        }

        public static Int64 Insert(Regency Regency)
        {
            try
            {
                var sql = @"INSERT INTO [Regencies] (Name, ProvincesID, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @ProvincesID, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Regency.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Regency);

                ///888 from integration
                //SystemLogRepository.Insert("Regency", Regency.ID, 888, Regency);
                return Regency.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Regency Regency)
        {
            try
            {
                var sql = @"UPDATE [Regencies] SET
                            Name = @Name,
                            ProvincesID = @ProvincesID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Regency, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Regency", Regency.ID, 888, Regency);
                return Regency.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
