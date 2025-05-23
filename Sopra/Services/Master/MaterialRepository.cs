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
    public class MaterialRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Material....");
            
            ///GET DATA FROM MYSQL
            var tableMaterial = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_materials WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableMaterial != null)
            {
                Trace.WriteLine($"Start Sync Material {tableMaterial.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableMaterial.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync MaterialID : {Convert.ToInt64(row["ID"])}");

                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Materials WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Material = new Material();

                            Material.RefID = Convert.ToInt64(row["id"]);
                            Material.Name = row["name"].ToString();
                            Material.PlasticType = row["plastic_type"].ToString();
                            Material.Halal = row["halal"] == DBNull.Value ? 0 : Convert.ToInt64(row["halal"]);
                            Material.FoodGrade = row["food_grade"] == DBNull.Value ? 0 : Convert.ToInt64(row["food_grade"]);
                            Material.BpaFree = row["bpa_free"] == DBNull.Value ? 0 : Convert.ToInt64(row["bpa_free"]);
                            Material.EcoFriendly = row["eco_friendly"] == DBNull.Value ? 0 : Convert.ToInt64(row["eco_friendly"]);
                            Material.Recyclable = row["recyclable"] == DBNull.Value ? 0 : Convert.ToInt64(row["recyclable"]);
                            Material.UserIn = 888;
                            MaterialRepository.Insert(Material);

                            Trace.WriteLine($"Success syncing MaterialID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing MaterialID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Material completed successfully.");
                    
                    //Utility.DeleteDiffData("mit_materials","Materials");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Material....");
        }

        public static Int64 Insert(Material Material)
        {
            try
            {
                var sql = @"INSERT INTO [Materials] (RefID, Name, PlasticType, Halal, FoodGrade, BpaFree, EcoFriendly, Recyclable,  DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Name, @PlasticType, @Halal, @FoodGrade, @BpaFree, @EcoFriendly, @Recyclable, @DateIn, @UserIn, @IsDeleted) ";
                Material.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Material);

                ///888 from integration
                //SystemLogRepository.Insert("Material", Material.ID, 888, Material);
                return Material.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Material Material)
        {
            try
            {
                var sql = @"UPDATE [Materials] SET
                            Name = @Name,
                            PlasticType = @PlasticType,
                            Halal = @Halal,
                            FoodGrade = @FoodGrade,
                            BpaFree = @BpaFree,
                            EcoFriendly = @EcoFriendly,
                            Recyclable = @Recyclable,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Material, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Material", Material.ID, 888, Material);
                return Material.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
