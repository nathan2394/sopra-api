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
    public class SearchQueryRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Search Query....");
            

            var tableSearchQuery = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_search_queries WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableSearchQuery != null)
            {
                Trace.WriteLine($"Start Sync Search Query {tableSearchQuery.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableSearchQuery.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync SearchQueryID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<SearchQuery>(string.Format("SELECT * FROM SearchQueries WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);
                            Utility.ExecuteNonQuery(string.Format("DELETE SearchQueries WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var SearchQuery = new SearchQuery();
                            if (obj != null) SearchQuery = obj;

                            SearchQuery.RefID = Convert.ToInt64(row["id"]);
                            SearchQuery.Keyword = row["keywords"] == DBNull.Value ? null : row["keywords"].ToString();
                            SearchQuery.SearchFrequency = row["search_frequency"] == DBNull.Value ? 0 : Convert.ToInt64(row["search_frequency"]);
                            SearchQueryRepository.Insert(SearchQuery);

                            

                            Trace.WriteLine($"Success syncing SearchQueryID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing SearchQueryID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Search Query completed successfully.");
                    //Utility.DeleteDiffData("mit_search_queries", "SearchQueries");
                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Search Query....");
        }

        public static Int64 Insert(SearchQuery SearchQuery)
        {
            try
            {
                var sql = @"INSERT INTO [SearchQueries] (Keyword, SearchFrequency, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Keyword, @SearchFrequency, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                SearchQuery.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, SearchQuery);

                ///888 from integration
                //SystemLogRepository.Insert("SearchQuery", SearchQuery.ID, 888, SearchQuery);
                return SearchQuery.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(SearchQuery SearchQuery)
        {
            try
            {
                var sql = @"UPDATE [SearchQueries] SET
                            Keyword = @Keyword,
                            SearchFrequency = @SearchFrequency,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, SearchQuery, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("SearchQuery", SearchQuery.ID, 888, SearchQuery);
                return SearchQuery.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
