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
    public class ProductCategoryRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Product Category....");

            Utility.ExecuteNonQuery("TRUNCATE TABLE ProductCategories");
            var tableProductCategory = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_product_categories ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableProductCategory != null)
            {
                Trace.WriteLine($"Start Sync Product Category {tableProductCategory.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableProductCategory.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ProductCategoryID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<ProductCategory>(string.Format("SELECT * FROM ProductCategories WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);
                            Utility.ExecuteNonQuery(string.Format("DELETE ProductCategories WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var ProductCategory = new ProductCategory();

                            ProductCategory.RefID = Convert.ToInt64(row["id"]);
                            ProductCategory.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            ProductCategory.Image = row["image"] == DBNull.Value ? null : row["image"].ToString();
                            ProductCategory.Keyword = row["keyword"] == DBNull.Value ? null : row["keyword"].ToString();
                            ProductCategory.Type = row["type"] == DBNull.Value ? null : row["type"].ToString();
                            ProductCategory.Seq = row["seq"] == DBNull.Value ? null : Convert.ToInt64(row["seq"]);
                            ProductCategoryRepository.Insert(ProductCategory);

                            Trace.WriteLine($"Success syncing ProductCategoryID : {Convert.ToInt64(row["ID"])}");
                            

                            Trace.WriteLine($"Delete Diff Data completed successfully.");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ProductCategoryID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Product Category completed successfully.");
                    //Utility.DeleteDiffData("mit_section_categories", "SectionCategories");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Product Category....");
        }

        public static Int64 Insert(ProductCategory ProductCategory)
        {
            try
            {
                var sql = @"INSERT INTO [ProductCategories] (RefID, Name, Image, Keyword, Type, Seq, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Name, @Image, @Keyword, @Type, @Seq, @DateIn, @UserIn, @IsDeleted) ";
                ProductCategory.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, ProductCategory);

                ///888 from integration
                //SystemLogRepository.Insert("ProductCategory", ProductCategory.ID, 888, ProductCategory);
                return ProductCategory.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(ProductCategory ProductCategory)
        {
            try
            {
                var sql = @"UPDATE [ProductCategories] SET
                            Name = @Name,
                            Image = @Image,
                            Keyword = @Keyword,
                            Type = @Type,
                            Seq = @Seq,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, ProductCategory, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("ProductCategory", ProductCategory.ID, 888, ProductCategory);
                return ProductCategory.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
