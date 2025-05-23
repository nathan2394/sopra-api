using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;

namespace SOPRA.Services
{
    public class ColorRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Color....");

            ///GET DATA FROM MYSQL
            var tableColors = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_colors WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableColors != null)
            {
                Trace.WriteLine($"Start Sync Color {tableColors.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableColors.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ColorID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Colors WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));
                            
                            var Color = new Color();
                            Color.RefID = Convert.ToInt64(row["id"]);
                            Color.Code = row["code"].ToString();
                            Color.Name = row["name"].ToString();
                            Color.UserIn = 888;
                            
                            ColorRepository.Insert(Color);
                            
                            Trace.WriteLine($"Success syncing ColorID : {Convert.ToInt64(row["ID"])}");

                            //Utility.DeleteDiffData("mit_colors","Colors");

                            //Trace.WriteLine($"Delete Diff Data completed successfully.");

                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ColorID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Color completed successfully.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Color....");
        }

        public static Int64 Insert(Color Color)
        {
            try
            {
                var sql = @"INSERT INTO [Colors] (Code, Name, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Code, @Name, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Color.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Color);

                ///888 from integration
                //SystemLogRepository.Insert("Color", Color.ID, 888, Color);
                return Color.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
