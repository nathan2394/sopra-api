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
    public class ProductKeywordRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Product Keyword....");

            Utility.ExecuteNonQuery("TRUNCATE TABLE ProductKeywords");
            var tableProductKeyword = Utility.MySqlGetObjects(string.Format("select * from mit_products_categories_key", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableProductKeyword != null)
            {
                Trace.WriteLine($"Start Sync Product Keyword {tableProductKeyword.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableProductKeyword.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ProductKeywordID : {Convert.ToInt64(row["ID"])}");
                            var ProductKeyword = new ProductKeyword();

                            //ProductKeyword.RefID = Convert.ToInt64(row["id"]);
                            ProductKeyword.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            ProductKeyword.NameEn = row["name_en"] == DBNull.Value ? null : row["name_en"].ToString();
                            ProductKeywordRepository.Insert(ProductKeyword);

                            Trace.WriteLine($"Success syncing ProductKeywordID : {Convert.ToInt64(row["ID"])}");


                            Trace.WriteLine($"Delete Diff Data completed successfully.");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ProductKeywordID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Product Keyword completed successfully.");
                    //Utility.DeleteDiffData("mit_products_categories_key", "ProductKeywords");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Product Keyword....");
        }

        public static Int64 Insert(ProductKeyword ProductKeyword)
        {
            try
            {
                var sql = @"INSERT INTO [ProductKeywords] (Name,NameEn) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name,@NameEn) ";
                ProductKeyword.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, ProductKeyword);

                ///888 from integration
                //SystemLogRepository.Insert("ProductKeyword", ProductKeyword.ID, 888, ProductKeyword);
                return ProductKeyword.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
