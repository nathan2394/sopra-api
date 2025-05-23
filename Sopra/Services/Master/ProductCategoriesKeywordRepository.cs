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
    public class ProductCategoriesKeywordRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Product Categories Keyword....");

            Utility.ExecuteNonQuery("TRUNCATE TABLE ProductCategoriesKeywords");
            var tableProductCategoriesKeyword = Utility.MySqlGetObjects(string.Format("select * from mit_products_categories", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableProductCategoriesKeyword != null)
            {
                Trace.WriteLine($"Start Sync Product Categories Keyword {tableProductCategoriesKeyword.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableProductCategoriesKeyword.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ProductCategoriesKeywordID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            var ProductCategoriesKeyword = new ProductCategoriesKeyword();

                            ProductCategoriesKeyword.ProductCategoriesID = row["mit_product_categories_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_product_categories_id"]);
                            ProductCategoriesKeyword.ProductKeywordID = row["mit_products_categories_key_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_products_categories_key_id"]);
                            
                            ProductCategoriesKeywordRepository.Insert(ProductCategoriesKeyword);

                            Trace.WriteLine($"Success syncing ProductCategoriesKeywordID : {Convert.ToInt64(row["ID"])}");


                            
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ProductCategoriesKeywordID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Product Categories Keyword completed successfully.");
                    //Utility.DeleteDiffData("mit_products_categories", "ProductCategoriesKeywords");
                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Product Categories Keyword....");
        }

        public static Int64 Insert(ProductCategoriesKeyword ProductCategoriesKeyword)
        {
            try
            {
                var sql = @"INSERT INTO [ProductCategoriesKeywords] (ProductCategoriesID, ProductKeywordID) 
                            OUTPUT INSERTED.ID
                            VALUES (@ProductCategoriesID, @ProductKeywordID) ";
                ProductCategoriesKeyword.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, ProductCategoriesKeyword);

                ///888 from integration
                //SystemLogRepository.Insert("ProductCategoriesKeyword", ProductCategoriesKeyword.ID, 888, ProductCategoriesKeyword);
                return ProductCategoriesKeyword.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
