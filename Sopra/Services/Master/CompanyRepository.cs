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
    public class CompanyRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Company....");
            

            var tableCompany = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_orders_companies WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCompany != null)
            {
                Trace.WriteLine($"Start Sync Company {tableCompany.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCompany.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CompanyID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Companies WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));


                            var Company = new Company();

                            Company.RefID = Convert.ToInt64(row["id"]);
                            Company.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            Company.PtName = row["pt_name"] == DBNull.Value ? null : row["pt_name"].ToString();
                            CompanyRepository.Insert(Company);

                            Trace.WriteLine($"Success syncing CompanyID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CompanyID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Company completed successfully.");
                    //Utility.DeleteDiffData("mit_orders_companies", "Companies");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Company....");
        }

        public static Int64 Insert(Company Company)
        {
            try
            {
                var sql = @"INSERT INTO [Companies] (RefID, Name, PtName, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Name, @PtName, @DateIn, @UserIn, @IsDeleted) ";
                Company.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Company);

                ///888 from integration
                //SystemLogRepository.Insert("Company", Company.ID, 888, Company);
                return Company.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Company Company)
        {
            try
            {
                var sql = @"UPDATE [Companies] SET
                            Name = @Name,
                            PtName = @PtName,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Company, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Company", Company.ID, 888, Company);
                return Company.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
