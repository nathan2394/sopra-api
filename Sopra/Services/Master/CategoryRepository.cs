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
    public class CategoryRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Category....");

            //GET DATA FROM MYSQL
            var tableCategoriesBottles = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_categories WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCategoriesBottles != null)
            {
                Trace.WriteLine($"Start Sync Category Bottles {tableCategoriesBottles.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCategoriesBottles.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CategoryID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Categories WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Bottles'", row["ID"]));

                            var category = new Category();
                            category.RefID = Convert.ToInt64(row["id"]);
                            category.Name = row["name"].ToString();
                            category.Type = "Bottles";
                            category.UserIn = 888;
                            CategoryRepository.Insert(category);

                            Trace.WriteLine($"Success syncing CategoryID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CategoryID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Category completed successfully.");
                    
                    //ID 25 = Hardcode Closures
                    //Utility.DeleteDiffData("mit_categories","Categories", "AND Type = 'Bottles' AND refID != 25");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //GET DATA FROM MYSQL
            var tableCategoriesThermos = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_thermo_categories WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCategoriesThermos != null)
            {
                Trace.WriteLine($"Start Sync Category Thermos {tableCategoriesThermos.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCategoriesThermos.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CategoryID : {Convert.ToInt64(row["ID"])}");
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Categories WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Thermos'", row["ID"]));
                            
                            var category = new Category();
                            category.RefID = Convert.ToInt64(row["id"]);
                            category.Name = row["name"].ToString();
                            category.Type = "Thermos";
                            category.UserIn = 888;

                            CategoryRepository.Insert(category);
                            
                            Trace.WriteLine($"Success syncing CategoryID : {Convert.ToInt64(row["ID"])}");
                            
                            Utility.DeleteDiffData("mit_categories","Categories", "AND Type = 'Thermos'");

                            Trace.WriteLine($"Delete Diff Data completed successfully.");

                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CategoryID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Category completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Category....");
        }

        public static Int64 Insert(Category category)
        {
            try
            {
                var sql = @"INSERT INTO [Categories] (Name, RefID, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RefID, @Type, @DateIn, @UserIn, @IsDeleted) ";
                category.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, category);

                return category.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
