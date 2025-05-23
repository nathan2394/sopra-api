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
using static Azure.Core.HttpHeader;
namespace Sopra.Services
{
    public class ReasonRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Reason....");
            

            var tableReason = Utility.MySqlGetObjects(string.Format("SELECT * FROM reason WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableReason != null)
            {
                Trace.WriteLine($"Start Sync Reason {tableReason.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableReason.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ReasonID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Reasons WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));


                            var Reason = new Reason();

                            Reason.RefID = Convert.ToInt64(row["id"]);
                            Reason.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            Reason.Type = row["type"] == DBNull.Value ? null : row["type"].ToString();
                            Reason.NameEN = row["name_eng"] == DBNull.Value ? null : row["name_eng"].ToString(); 
                            ReasonRepository.Insert(Reason);

                            Trace.WriteLine($"Success syncing ReasonID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ReasonID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Reason completed successfully.");
                    //Utility.DeleteDiffData("reason", "Reasons");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Reason....");
        }

        public static Int64 Insert(Reason Reason)
        {
            try
            {
                var sql = @"INSERT INTO [Reasons] (RefID, Name,NameEN, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Name,@NameEN, @Type, @DateIn, @UserIn, @IsDeleted) ";
                Reason.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Reason);

                ///888 from integration
                //SystemLogRepository.Insert("Reason", Reason.ID, 888, Reason);
                return Reason.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Reason Reason)
        {
            try
            {
                var sql = @"UPDATE [Reasons] SET
                            Name = @Name,
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Reason, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Reason", Reason.ID, 888, Reason);
                return Reason.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
