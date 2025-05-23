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
    public class CountryRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Country....");
            Utility.ExecuteNonQuery(string.Format("TRUNCATE TABLE Countries"));
            var tableCountry = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_country ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCountry != null)
            {
                Trace.WriteLine($"Start Sync Country {tableCountry.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCountry.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CountryID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            
                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<Country>(string.Format("SELECT * FROM Countries WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var Country = new Country();
                            if (obj != null) Country = obj;

                            Country.RefID = Convert.ToInt64(row["id"]);
                            Country.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();

                            if (obj == null)
                            {
                                // INSERT
                                Country.UserIn = 888;
                                CountryRepository.Insert(Country);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS Country
                                Country.UserUp = 888;
                                CountryRepository.Update(Country);
                            }

                            Trace.WriteLine($"Success syncing CountryID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CountryID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Country completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Country....");
        }

        public static Int64 Insert(Country Country)
        {
            try
            {
                var sql = @"INSERT INTO [Countries] (Name, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Country.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Country);

                ///888 from integration
                //SystemLogRepository.Insert("Country", Country.ID, 888, Country);
                return Country.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Country Country)
        {
            try
            {
                var sql = @"UPDATE [Countries] SET
                            Name = @Name,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Country, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Country", Country.ID, 888, Country);
                return Country.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
