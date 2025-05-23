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
    public class DistrictRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync District....");


            var tableDistrict = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_districts WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableDistrict != null)
            {
                Trace.WriteLine($"Start Sync District {tableDistrict.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableDistrict.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync DistrictID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Districts WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var District = new District();

                            District.RefID = Convert.ToInt64(row["id"]);
                            District.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            District.RegenciesID = row["regency_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["regency_id"]);
                            DistrictRepository.Insert(District);

                            Trace.WriteLine($"Success syncing DistrictID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing DistrictID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization District completed successfully.");
                    //Utility.DeleteDiffData("mit_districts", "Districts");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync District....");
        }

        public static Int64 Insert(District District)
        {
            try
            {
                var sql = @"INSERT INTO [Districts] (Name, RegenciesID, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RegenciesID, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                District.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, District);

                ///888 from integration
                //SystemLogRepository.Insert("District", District.ID, 888, District);
                return District.ID;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error inserting District: {District.Name} - {ex.Message}");
                throw;
            }
        }

        public static Int64 Update(District District)
        {
            try
            {
                var sql = @"UPDATE [Districts] SET
                            Name = @Name,
                            RegenciesID = @RegenciesID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, District, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("District", District.ID, 888, District);
                return District.ID;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error updating District: {District.Name} - {ex.Message}");
                throw;
            }
        }
    }
}
