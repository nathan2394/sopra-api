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
    public class SearchKeywordRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Search Keyword....");
            

            var tableSearchKeyword = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_search_keyword WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableSearchKeyword != null)
            {
                Trace.WriteLine($"Start Sync Search Keyword {tableSearchKeyword.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableSearchKeyword.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync SearchKeywordID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE SearchKeywords WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var SearchKeyword = new SearchKeyword();

                            SearchKeyword.RefID = Convert.ToInt64(row["id"]);
                            SearchKeyword.Keyword = row["keyword"] == DBNull.Value ? null : row["keyword"].ToString();
                            SearchKeyword.CorrectKeyword = row["correct_keyword"] == DBNull.Value ? null : row["correct_keyword"].ToString();
                            SearchKeywordRepository.Insert(SearchKeyword);
                            

                            Trace.WriteLine($"Success syncing SearchKeywordID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing SearchKeywordID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Search Keyword completed successfully.");
                    //Utility.DeleteDiffData("mit_search_keyword", "SearchKeywords");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Search Keyword....");
        }

        public static Int64 Insert(SearchKeyword SearchKeyword)
        {
            try
            {
                var sql = @"INSERT INTO [SearchKeywords] (Keyword, CorrectKeyword, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Keyword, @CorrectKeyword, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                SearchKeyword.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, SearchKeyword);

                ///888 from integration
                //SystemLogRepository.Insert("SearchKeyword", SearchKeyword.ID, 888, SearchKeyword);
                return SearchKeyword.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(SearchKeyword SearchKeyword)
        {
            try
            {
                var sql = @"UPDATE [SearchKeywords] SET
                            Keyword = @Keyword,
                            CorrectKeyword = @CorrectKeyword,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, SearchKeyword, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("SearchKeyword", SearchKeyword.ID, 888, SearchKeyword);
                return SearchKeyword.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
