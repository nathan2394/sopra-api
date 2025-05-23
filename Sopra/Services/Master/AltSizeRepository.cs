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

namespace Sopra.Services.Master
{
    public class AltSizeRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Size....");
            
            var tableAltSize = Utility.MySqlGetObjects(string.Format("select * from mit_alt_size WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltSize != null)
            {
                Trace.WriteLine($"Start Sync Alt Size {tableAltSize.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltSize.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltSizeID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE AltSizes WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var AltSize = new AltSize();

                            AltSize.RefID = Convert.ToInt64(row["id"]);
                            AltSize.Group = row["group"] == DBNull.Value ? null : row["group"].ToString();
                            AltSize.Code = row["code"] == DBNull.Value ? null : row["code"].ToString();
                            AltSizeRepository.Insert(AltSize);

                            Trace.WriteLine($"Success syncing AltSizeID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltSizeID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Size completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_size", "AltSizes");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Alt Size....");
        }

        public static Int64 Insert(AltSize AltSize)
        {
            try
            {
                var sql = @"INSERT INTO [AltSizes] (Code,[Group], RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Code,@Group, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                AltSize.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltSize);

                ///888 from integration
                //SystemLogRepository.Insert("AltSize", AltSize.ID, 888, AltSize);
                return AltSize.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(AltSize AltSize)
        {
            try
            {
                var sql = @"UPDATE [AltSizes] SET
                            [Group] = @Group,
                            Code = @Code,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltSize, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltSize", AltSize.ID, 888, AltSize);
                return AltSize.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
