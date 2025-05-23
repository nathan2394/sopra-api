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
    public class CategoryDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Category Detail....");
            var deleteData = Utility.SQLDBConnection.QueryFirstOrDefault<CategoryDetail>(string.Format("TRUNCATE TABLE CategoryDetails"), transaction: null);
            //GET DATA FROM MYSQL
            var tableCategoryDetail = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_product_icon WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCategoryDetail != null)
            {
                Trace.WriteLine($"Start Sync Category Detail {tableCategoryDetail.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCategoryDetail.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CategoryDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            //var obj = Utility.SQLDBConnection.QueryFirstOrDefault<CategoryDetail>(string.Format("SELECT * FROM CategoryDetails WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var CategoryDetail = new CategoryDetail();
                            //if (obj != null) CategoryDetail = obj;

                            //if (row.Table.Columns.Contains("beverage_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 2;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["beverage_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("food_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 1;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["food_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("personalCare_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 7;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["personalCare_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("cosmetics_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 3;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["cosmetics_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("pharmaceutical_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 4;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["pharmaceutical_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("chemical_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 5;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["chemical_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("jar_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 6;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["jar_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("homeCare_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 8;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["homeCare_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("petCan_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 15;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["petCan_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("closures_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 25;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["closures_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Bottles";
                            //}

                            //if (row.Table.Columns.Contains("tray_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 4;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["tray_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Thermos";
                            //}

                            //if (row.Table.Columns.Contains("cup_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 5;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["cup_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Thermos";
                            //}

                            //if (row.Table.Columns.Contains("lid_image"))
                            //{
                            //    CategoryDetail.RefID = Convert.ToInt64(row["id"]);
                            //    CategoryDetail.CategoriesID = 6;
                            //    CategoryDetail.Name = row["name"].ToString();
                            //    CategoryDetail.Image = row["lid_image"].ToString();
                            //    CategoryDetail.Color = row["color"].ToString();
                            //    CategoryDetail.Type = "Thermos";
                            //}

                            //if (obj == null)
                            //{
                            // INSERT
                            CategoryDetail.UserIn = 888;
                            CategoryDetailRepository.Insert(CategoryDetail);
                            //}
                            //else
                            //{
                            //    // UPDATE
                            //    // MAPPING KE CLASS CategoryDetail
                            //    CategoryDetail.UserUp = 888;
                            //    CategoryDetailRepository.Update(CategoryDetail);
                            //}

                            Trace.WriteLine($"Success syncing CategoryDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CategoryDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Category Detail completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Category Detail....");
        }

        public static Int64 Insert(CategoryDetail CategoryDetail)
        {
            try
            {
                var sql = @"INSERT INTO [CategoryDetails] (RefID, CategoriesID, Name, Image, Color, Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @CategoriesID, @Name, @Image, @Color, @Type, @DateIn, @UserIn, @IsDeleted) ";
                CategoryDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, CategoryDetail);

                ///888 from integration
                //SystemLogRepository.Insert("CategoryDetail", CategoryDetail.ID, 888, CategoryDetail);
                return CategoryDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(CategoryDetail CategoryDetail)
        {
            try
            {
                var sql = @"UPDATE [CategoryDetails] SET
                            CategoriesID = @CategoriesID,
                            Name = @Name,
                            Image = @Image,
                            Color = @Color, 
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, CategoryDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("CategoryDetail", CategoryDetail.ID, 888, CategoryDetail);
                return CategoryDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
