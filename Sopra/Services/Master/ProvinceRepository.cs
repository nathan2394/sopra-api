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
    public class ProvinceRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Province....");
            

            var tableProvince = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_provinces WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableProvince != null)
            {
                Trace.WriteLine($"Start Sync Province {tableProvince.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableProvince.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ProvinceID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Provinces WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Province = new Province();

                            Province.RefID = Convert.ToInt64(row["id"]);
                            Province.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            Province.CountriesID = row["country_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["country_id"]);
                            ProvinceRepository.Insert(Province);

                            Trace.WriteLine($"Success syncing ProvinceID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ProvinceID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Province completed successfully.");
                    Utility.DeleteDiffData("mit_provinces", "Provinces");

                    Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Province....");
        }

        public static Int64 Insert(Province Province)
        {
            try
            {
                var sql = @"INSERT INTO [Provinces] (Name, CountriesID, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @CountriesID, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Province.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Province);

                ///888 from integration
                //SystemLogRepository.Insert("Province", Province.ID, 888, Province);
                return Province.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Province Province)
        {
            try
            {
                var sql = @"UPDATE [Provinces] SET
                            Name = @Name,
                            CountriesID = @CountriesID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Province, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Province", Province.ID, 888, Province);
                return Province.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
