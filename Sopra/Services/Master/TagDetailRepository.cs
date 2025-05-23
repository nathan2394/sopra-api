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
    public class TagDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Tag Detail Bottle....");
            

            var tableTagDetailBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_products_tags WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableTagDetailBottle != null)
            {
                Trace.WriteLine($"Start Sync Tag Detail Bottle {tableTagDetailBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableTagDetailBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync TagDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE TagDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type ='Bottle'", row["ID"]));

                            var TagDetail = new TagDetail();

                            TagDetail.RefID = Convert.ToInt64(row["id"]);
                            TagDetail.TagsID = row["mit_tags_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_tags_id"]);
                            TagDetail.ObjectID = row["mit_calculators_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_calculators_id"]);
                            TagDetail.Type = "Bottle";
                            TagDetailRepository.Insert(TagDetail);

                            Trace.WriteLine($"Success syncing TagDetailID : {Convert.ToInt64(row["ID"])}");
                            //Utility.DeleteDiffData("mit_products_tags", "TagDetails", "AND Type ='Bottle'");

                            //Trace.WriteLine($"Delete Diff Data completed successfully.");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing TagDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Tag Detail Bottle completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //var tableTagDetail = Utility.MySqlGetObjects(string.Format("SELECT mit_thermos_tags.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_volume_detail LEFT JOIN mit_products ON mit_alt_volume_detail.products_id = mit_products.id WHERE (mit_alt_volume_detail.updated_at is null AND mit_alt_volume_detail.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mit_alt_volume_detail.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            var tableTagDetailThermo = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_thermos_tags WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableTagDetailThermo != null)
            {
                Trace.WriteLine($"Start Sync Tag Detail Thermo {tableTagDetailThermo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableTagDetailThermo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync TagDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE TagDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type ='Thermo'", row["ID"]));

                            var TagDetail = new TagDetail();

                            TagDetail.RefID = Convert.ToInt64(row["id"]);
                            TagDetail.TagsID = row["mit_tags_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_tags_id"]);
                            TagDetail.ObjectID = row["mit_thermos_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_thermos_id"]);
                            TagDetail.Type = "Thermo";


                            Trace.WriteLine($"Success syncing TagDetailID : {Convert.ToInt64(row["ID"])}");
                            
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing TagDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Tag Detail Thermo completed successfully.");
                    //Utility.DeleteDiffData("mit_thermos_tags", "TagDetails", "AND Type ='Thermo'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //var tableTagDetail = Utility.MySqlGetObjects(string.Format("SELECT mit_lids_tags.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_volume_detail LEFT JOIN mit_products ON mit_alt_volume_detail.products_id = mit_products.id WHERE (mit_alt_volume_detail.updated_at is null AND mit_alt_volume_detail.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mit_alt_volume_detail.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            var tableTagDetailLid = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_lids_tags", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableTagDetailLid != null)
            {
                Trace.WriteLine($"Start Sync Tag Detail Lid {tableTagDetailLid.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableTagDetailLid.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync TagDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            //var obj = Utility.SQLDBConnection.QueryFirstOrDefault<TagDetail>(string.Format("SELECT * FROM TagDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type ='Lid'", row["ID"]), transaction: null);

                            var TagDetail = new TagDetail();

                            TagDetail.RefID = Convert.ToInt64(row["id"]);
                            TagDetail.TagsID = row["mit_tags_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_tags_id"]);
                            TagDetail.ObjectID = row["mit_lids_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_lids_id"]);
                            TagDetail.Type = "Lid";


                            Trace.WriteLine($"Success syncing TagDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing TagDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Tag Detail Lid completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Tag....");
        }

        public static Int64 Insert(TagDetail TagDetail)
        {
            try
            {
                var sql = @"INSERT INTO [TagDetails] (TagsID, ObjectID, Type, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@TagsID, @ObjectID, @Type, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                TagDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, TagDetail);

                ///888 from integration
                //SystemLogRepository.Insert("TagDetail", TagDetail.ID, 888, TagDetail);
                return TagDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(TagDetail TagDetail)
        {
            try
            {
                var sql = @"UPDATE [TagDetails] SET
                            TagsID = @TagsID,
                            ObjectID = @ObjectID,
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, TagDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("TagDetail", TagDetail.ID, 888, TagDetail);
                return TagDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
