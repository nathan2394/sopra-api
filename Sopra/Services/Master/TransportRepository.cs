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
    public class TransportRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Transport....");
            

            var tableTransport = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_transports WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableTransport != null)
            {
                Trace.WriteLine($"Start Sync Transport {tableTransport.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableTransport.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync TransportID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Transports WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Transport = new Transport();

                            Transport.RefID = Convert.ToInt64(row["id"]);
                            Transport.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            Transport.Cost = row["cost"] == DBNull.Value ? 0 : Convert.ToDecimal(row["cost"]);
                            Transport.Length = row["length"] == DBNull.Value ? 0 : Convert.ToDecimal(row["length"]);
                            Transport.Width = row["width"] == DBNull.Value ? 0 : Convert.ToDecimal(row["width"]);
                            Transport.Height = row["height"] == DBNull.Value ? 0 : Convert.ToDecimal(row["height"]);
                            Transport.Type = row["type"] == DBNull.Value ? 0 : Convert.ToInt64(row["type"]);
                            TransportRepository.Insert(Transport);

                            Trace.WriteLine($"Success syncing TransportID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing TransportID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Transport completed successfully.");
                    //Utility.DeleteDiffData("mit_transports", "Transports");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Transport....");
        }

        public static Int64 Insert(Transport Transport)
        {
            try
            {
                var sql = @"INSERT INTO [Transports] (RefID, Name, Cost, Length, Width, Height, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Name, @Cost, @Length, @Width, @Height, @Type, @DateIn, @UserIn, @IsDeleted) ";
                Transport.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Transport);

                ///888 from integration
                //SystemLogRepository.Insert("Transport", Transport.ID, 888, Transport);
                return Transport.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Transport Transport)
        {
            try
            {
                var sql = @"UPDATE [Transports] SET
                            Name = @Name,
                            Cost = @Cost,
                            Length = @Length,
                            Width = @Width,
                            Height = @Height,
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Transport, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Transport", Transport.ID, 888, Transport);
                return Transport.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
