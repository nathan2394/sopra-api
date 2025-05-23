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
    public class TagRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Tag....");
            

            var tableTag = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_tags WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableTag != null)
            {
                Trace.WriteLine($"Start Sync Tag {tableTag.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableTag.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync TagID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Tags WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Tag = new Tag();

                            Tag.RefID = Convert.ToInt64(row["id"]);
                            Tag.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            TagRepository.Insert(Tag);
                            

                            Trace.WriteLine($"Success syncing TagID : {Convert.ToInt64(row["ID"])}");
                            
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing TagID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Tag completed successfully.");
                    //Utility.DeleteDiffData("mit_tags", "Tags");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Tag....");
        }

        public static Int64 Insert(Tag Tag)
        {
            try
            {
                var sql = @"INSERT INTO [Tags] (Name, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Tag.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Tag);

                ///888 from integration
                //SystemLogRepository.Insert("Tag", Tag.ID, 888, Tag);
                return Tag.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Tag Tag)
        {
            try
            {
                var sql = @"UPDATE [Tags] SET
                            Name = @Name,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Tag, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Tag", Tag.ID, 888, Tag);
                return Tag.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
