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
    public class FunctionRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Function....");
            

            var tableFunction = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_functions WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableFunction != null)
            {
                Trace.WriteLine($"Start Sync Function {tableFunction.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableFunction.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync FunctionID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Functions WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Function = new Function();

                            Function.RefID = Convert.ToInt64(row["id"]);
                            Function.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            FunctionRepository.Insert(Function);

                            Trace.WriteLine($"Success syncing FunctionID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing FunctionID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Function completed successfully.");
                    //Utility.DeleteDiffData("mit_functions", "Functions");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Function....");
        }

        public static Int64 Insert(Function Function)
        {
            try
            {
                var sql = @"INSERT INTO [Functions] (Name, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Function.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Function);

                ///888 from integration
                //SystemLogRepository.Insert("Function", Function.ID, 888, Function);
                return Function.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Function Function)
        {
            try
            {
                var sql = @"UPDATE [Functions] SET
                            Name = @Name,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Function, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Function", Function.ID, 888, Function);
                return Function.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
