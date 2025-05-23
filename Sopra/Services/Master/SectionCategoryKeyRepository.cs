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
    public class SectionCategoryKeyRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Section Category Key....");

            Utility.ExecuteNonQuery("TRUNCATE TABLE SectionCategoryKeys");
            var tableSectionCategoryKey = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_section_categories_key ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableSectionCategoryKey != null)
            {
                Trace.WriteLine($"Start Sync Section Category Key {tableSectionCategoryKey.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableSectionCategoryKey.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync SectionCategoryKeyID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE SectionCategoryKeys WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var SectionCategoryKey = new SectionCategoryKey();

                            SectionCategoryKey.RefID = Convert.ToInt64(row["id"]);
                            SectionCategoryKey.SectionCategoriesID = row["mit_section_categories_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_section_categories_id"]);
                            SectionCategoryKey.PoductCategoriesID = row["mit_product_categories_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_product_categories_id"]);
                            SectionCategoryKey.ImgTheme = row["img_theme"] == DBNull.Value ? null : row["img_theme"].ToString();
                            SectionCategoryKey.Description = row["description"] == DBNull.Value ? null : row["description"].ToString();
                            SectionCategoryKeyRepository.Insert(SectionCategoryKey);

                            Trace.WriteLine($"Success syncing SectionCategoryKeyID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing SectionCategoryKeyID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Section Category Key completed successfully.");
                    //Utility.DeleteDiffData("mit_section_categories_key", "SectionCategoryKeys");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Section Category Key....");
        }

        public static Int64 Insert(SectionCategoryKey SectionCategoryKey)
        {
            try
            {
                var sql = @"INSERT INTO [SectionCategoryKeys] (RefID, SectionCategoriesID, PoductCategoriesID, ImgTheme, Description, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @SectionCategoriesID, @PoductCategoriesID, @ImgTheme, @Description, @DateIn, @UserIn, @IsDeleted) ";
                SectionCategoryKey.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, SectionCategoryKey);

                ///888 from integration
                //SystemLogRepository.Insert("SectionCategoryKey", SectionCategoryKey.ID, 888, SectionCategoryKey);
                return SectionCategoryKey.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(SectionCategoryKey SectionCategoryKey)
        {
            try
            {
                var sql = @"UPDATE [SectionCategoryKeys] SET
                            SectionCategoriesID = @SectionCategoriesID,
                            PoductCategoriesID = @PoductCategoriesID,
                            ImgTheme = @ImgTheme,
                            Description = @Description,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, SectionCategoryKey, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("SectionCategoryKey", SectionCategoryKey.ID, 888, SectionCategoryKey);
                return SectionCategoryKey.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
