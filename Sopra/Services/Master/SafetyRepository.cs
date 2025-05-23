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
//using MySqlX.XDevAPI.Common;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;

namespace SOPRA.Services
{
    public class SafetyRepository
    {
        public static void Sync()
        {

            Trace.WriteLine("Running Sync Safety....");
            
            ///GET DATA FROM MYSQL
            var tableFoodSafeties = Utility.MySqlGetObjects(string.Format("SELECT * FROM food_safety WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableFoodSafeties != null)
            {
                Trace.WriteLine($"Start Sync Food Safety {tableFoodSafeties.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableFoodSafeties.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync SafetyID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Safeties WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Food'", row["ID"]));

                            var Safety = new Safety();

                            Safety.RefID = Convert.ToInt64(row["id"]);
                            Safety.Type = "Food";
                            Safety.Name = row["name"].ToString();
                            Safety.Image = row["image"].ToString();
                            Safety.Note = row["note"].ToString();
                            Safety.UserIn = 888;
                            SafetyRepository.Insert(Safety);

                            Trace.WriteLine($"Success syncing SafetyID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing SafetyID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }

                Trace.WriteLine($"Synchronization Safeties completed successfully.");

                //Utility.DeleteDiffData("food_safety","Safeties","AND Type = 'Food'");

                //Trace.WriteLine($"Delete Diff Data completed successfully.");

                Thread.Sleep(100);
            }

             var tableMaterialSafeties = Utility.MySqlGetObjects(string.Format("SELECT * FROM material_safety WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableMaterialSafeties != null)
            {
                Trace.WriteLine($"Start Sync Material Safety {tableMaterialSafeties.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableMaterialSafeties.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync SafetyID : {Convert.ToInt64(row["ID"])}");

                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Necks WHERE Safeties = {0} AND IsDeleted = 0 AND Type = 'Material'", row["ID"]));

                            var Safety = new Safety();

                            Safety.RefID = Convert.ToInt64(row["id"]);
                            Safety.Type = "Material";
                            Safety.Name = row["name"].ToString();
                            Safety.Image = row["image"].ToString();
                            Safety.Note = row["note"].ToString();
                            Safety.UserIn = 888;
                            SafetyRepository.Insert(Safety);

                            Trace.WriteLine($"Success syncing SafetyID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing SafetyID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }

                Trace.WriteLine($"Synchronization Safeties completed successfully.");

                Utility.DeleteDiffData("food_safety","Safeties","AND Type = 'Material'");

                Trace.WriteLine($"Delete Diff Data completed successfully.");

                Thread.Sleep(100);
            }

            Trace.WriteLine("Finished Sync Safety....");
        }

        public static Int64 Insert(Safety Safety)
        {
            try
            {
                var sql = @"INSERT INTO [Safeties] ( RefID, Type, Name, Image, Note, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Type, @Name, @Image, @Note, @DateIn, @UserIn, @IsDeleted) ";
                Safety.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Safety);

                ///888 from integration
                //SystemLogRepository.Insert("Safety", Safety.ID, 888, Safety);
                return Safety.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
