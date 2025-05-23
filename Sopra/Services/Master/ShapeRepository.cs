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
    public class ShapeRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Shape....");
            
            //GET DATA FROM MYSQL
            var tableCategories = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_shapes WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCategories != null)
            {
                Trace.WriteLine($"Start Sync Shape {tableCategories.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCategories.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ShapeID : {Convert.ToInt64(row["ID"])}");

                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Shapes WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Shape = new Shape();
                            Shape.RefID = Convert.ToInt64(row["id"]);
                            Shape.Name = row["name"].ToString();
                            Shape.Type = row["type"].ToString();
                            Shape.UserIn = 888;
                            
                            ShapeRepository.Insert(Shape);

                            Trace.WriteLine($"Success syncing ShapeID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ShapeID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Neck completed successfully.");

                    //Utility.DeleteDiffData("mit_shapes","Shapes");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Shape....");
        }

        public static Int64 Insert(Shape Shape)
        {
            try
            {
                var sql = @"INSERT INTO [Shapes] (Name, RefID, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RefID, @Type, @DateIn, @UserIn, @IsDeleted) ";
                Shape.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Shape);

                ///888 from integration
                //SystemLogRepository.Insert("Shape", Shape.ID, 888, Shape);
                return Shape.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
