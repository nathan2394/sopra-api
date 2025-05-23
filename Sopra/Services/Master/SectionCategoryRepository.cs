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
    public class SectionCategoryRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Section Category....");

            Utility.ExecuteNonQuery("TRUNCATE TABLE SectionCategories");
            var tableSectionCategory = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_section_categories ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableSectionCategory != null)
            {
                Trace.WriteLine($"Start Sync Section Category {tableSectionCategory.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableSectionCategory.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync SectionCategoryID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE SectionCategories WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var SectionCategory = new SectionCategory();

                            SectionCategory.RefID = Convert.ToInt64(row["id"]);
                            SectionCategory.SectionTitle = row["section_title"] == DBNull.Value ? null : row["section_title"].ToString();
                            SectionCategory.SectionTitleEN = row["section_title_en"] == DBNull.Value ? null : row["section_title_en"].ToString();
                            SectionCategory.Seq = row["seq"] == DBNull.Value ? 0 : Convert.ToInt64(row["seq"]);
                            SectionCategory.Status = row["status"] == DBNull.Value ? null : row["status"].ToString();
                            SectionCategory.ImgBannerDesktop = row["img_banner_desktop"] == DBNull.Value ? null : row["img_banner_desktop"].ToString();
                            SectionCategory.ImgBannerMobile = row["img_banner_mobile"] == DBNull.Value ? null : row["img_banner_mobile"].ToString();
                            //SectionCategory.Description = row["description"] == DBNull.Value ? null : row["description"].ToString();
                            SectionCategoryRepository.Insert(SectionCategory);
                            

                            Trace.WriteLine($"Success syncing SectionCategoryID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing SectionCategoryID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Section Category completed successfully.");
                    //Utility.DeleteDiffData("mit_section_categories", "SectionCategories");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Section Category....");
        }

        public static Int64 Insert(SectionCategory SectionCategory)
        {
            try
            {
                var sql = @"INSERT INTO [SectionCategories] (RefID, SectionTitle,SectionTitleEN, Seq, Status, ImgBannerDesktop, ImgBannerMobile, Description, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @SectionTitle,@SectionTitleEN, @Seq, @Status, @ImgBannerDesktop, @ImgBannerMobile, @Description, @DateIn, @UserIn, @IsDeleted) ";
                SectionCategory.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, SectionCategory);

                ///888 from integration
                //SystemLogRepository.Insert("SectionCategory", SectionCategory.ID, 888, SectionCategory);
                return SectionCategory.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(SectionCategory SectionCategory)
        {
            try
            {
                var sql = @"UPDATE [SectionCategories] SET
                            SectionTitle = @SectionTitle,
                            Seq = @Seq,
                            Status = @Status,
                            ImgBannerDesktop = @ImgBannerDesktop,
                            ImgBannerMobile = @ImgBannerMobile,
                            Description = @Description,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, SectionCategory, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("SectionCategory", SectionCategory.ID, 888, SectionCategory);
                return SectionCategory.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
