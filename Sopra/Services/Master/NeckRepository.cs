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
    public class NeckRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Neck....");

            //GET DATA FROM MYSQL
            var tableCategories = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_Necks WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCategories != null)
            {
                Trace.WriteLine($"Start Sync Neck {tableCategories.Rows.Count} Data(s)....");

                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCategories.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync NeckID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Necks WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Neck = new Neck();
                            Neck.RefID = Convert.ToInt64(row["id"]);
                            Neck.Code = row["code"].ToString();
                            Neck.Type = row["type"].ToString();
                            Neck.UserIn = 888;

                            NeckRepository.Insert(Neck);

                            Trace.WriteLine($"Success syncing NeckID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing NeckID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Neck completed successfully.");

                    //Utility.DeleteDiffData("mit_necks","necks");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Neck....");
        }

        public static Int64 Insert(Neck Neck)
        {
            try
            {
                var sql = @"INSERT INTO [Necks] (Code, RefID, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Code, @RefID, @Type, @DateIn, @UserIn, @IsDeleted) ";
                Neck.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Neck);

                ///888 from integration
                //SystemLogRepository.Insert("Neck", Neck.ID, 888, Neck);
                return Neck.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        } 
    }
}
