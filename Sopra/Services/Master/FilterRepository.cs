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

namespace Sopra.Services.Master
{
    public class FilterRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Filter....");
            

            var tableFilter = Utility.MySqlGetObjects(string.Format("SELECT mit_alt_neck_detail.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_neck_detail LEFT JOIN mit_products ON mit_alt_neck_detail.products_id = mit_products.id ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableFilter != null)
            {
                Trace.WriteLine($"Start Sync Filter {tableFilter.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableFilter.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync FilterID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            
                            Utility.ExecuteNonQuery(string.Format("DELETE Filters WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Filter = new Filter();

                            Filter.RefID = Convert.ToInt64(row["id"]);
                            Filter.CategoryID = row["alt_neck_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["alt_neck_id"]);
                            Filter.Min = row["alt_neck_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["alt_neck_id"]); 
                            Filter.Max = row["alt_neck_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["alt_neck_id"]); 
                            Filter.Type = row["alt_neck_id"] == DBNull.Value ? string.Empty : Convert.ToString(row["alt_neck_id"]);
                            FilterRepository.Insert(Filter);

                            Trace.WriteLine($"Success syncing FilterID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing FilterID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Neck completed successfully.");
                    Utility.DeleteDiffData("mit_section_categories", "SectionCategories");

                    Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Alt Neck....");
        }

        public static Int64 Insert(Filter Filter)
        {
            try
            {
                var sql = @"INSERT INTO [Filters] (Min,Max,Type, CategoryID, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Min,@Max,@Type, @CategoryID,@RefID, @DateIn, @UserIn, @IsDeleted) ";
                Filter.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Filter);

                ///888 from integration
                //SystemLogRepository.Insert("Filter", Filter.ID, 888, Filter);
                return Filter.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Filter Filter)
        {
            try
            {
                var sql = @"UPDATE [Filters] SET
                            Min = @Min,
                            Max = @Max,
                            Type = @Type, 
                            CategoryID = @CategoryID
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Filter, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Filter", Filter.ID, 888, Filter);
                return Filter.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
