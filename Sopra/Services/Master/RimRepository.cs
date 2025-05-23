using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;

namespace SOPRA.Services
{
    public class RimRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Rim....");
            
            ///GET DATA FROM MYSQL
            var tableRims = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_rims WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableRims != null)
            {
                Trace.WriteLine($"Start Sync Rim {tableRims.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableRims.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync RimID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Rims WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));
                            
                            var Rim = new Rim();
                            Rim.RefID = Convert.ToInt64(row["ID"]);
                            Rim.Name = row["Name"].ToString();
                            Rim.UserIn = 888;
                            RimRepository.Insert(Rim);
                            
                            Trace.WriteLine($"Success syncing RimID : {Convert.ToInt64(row["ID"])}");

                            //Utility.DeleteDiffData("mit_rims","Rims");

                            //Trace.WriteLine($"Delete Diff Data completed successfully.");

                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing RimID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Rim completed successfully.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Rim....");
        }

        public static Int64 Insert(Rim Rim)
        {
            try
            {
                var sql = @"INSERT INTO [Rims] (Name, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Rim.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Rim);

                ///888 from integration
                //SystemLogRepository.Insert("Rim", Rim.ID, 888, Rim);
                return Rim.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
