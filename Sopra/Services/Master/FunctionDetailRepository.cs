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
    public class FunctionDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Function Detail Bottle....");
            

            var tableFunctionDetailBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_products_functions WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableFunctionDetailBottle != null)
            {
                Trace.WriteLine($"Start Sync Function Detail Bottle {tableFunctionDetailBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableFunctionDetailBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync FunctionDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE FunctionDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Bottle'", row["ID"]));

                            var FunctionDetail = new FunctionDetail();

                            FunctionDetail.RefID = Convert.ToInt64(row["id"]);
                            FunctionDetail.FunctionsID = row["mit_functions_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_functions_id"]);
                            FunctionDetail.ObjectID = row["mit_calculators_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_calculators_id"]);
                            FunctionDetail.Type = "Bottle";
                            FunctionDetailRepository.Insert(FunctionDetail);

                            Trace.WriteLine($"Success syncing FunctionDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing FunctionDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Function Detail Bottle completed successfully.");
                    //Utility.DeleteDiffData("mit_products_functions", "FunctionDetails", "AND Type = 'Bottle'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //var tableFunctionDetail = Utility.MySqlGetObjects(string.Format("SELECT mit_thermos_functions.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_volume_detail LEFT JOIN mit_products ON mit_alt_volume_detail.products_id = mit_products.id WHERE (mit_alt_volume_detail.updated_at is null AND mit_alt_volume_detail.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mit_alt_volume_detail.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            var tableFunctionDetailThermo = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_thermos_functions WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableFunctionDetailThermo != null)
            {
                Trace.WriteLine($"Start Sync Function Detail Thermo {tableFunctionDetailThermo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableFunctionDetailThermo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync FunctionDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE FunctionDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type ='Thermo'", row["ID"]));

                            var FunctionDetail = new FunctionDetail();

                            FunctionDetail.RefID = Convert.ToInt64(row["id"]);
                            FunctionDetail.FunctionsID = row["mit_functions_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_functions_id"]);
                            FunctionDetail.ObjectID = row["mit_thermos_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_thermos_id"]);
                            FunctionDetail.Type = "Thermo";
                            FunctionDetailRepository.Insert(FunctionDetail);


                            Trace.WriteLine($"Success syncing FunctionDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing FunctionDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }


                    Trace.WriteLine($"Synchronization Function Detail Thermo completed successfully.");
                    //Utility.DeleteDiffData("mit_carts_detail_thermo", "CartDetails", "AND Type = 'Thermo'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //var tableFunctionDetail = Utility.MySqlGetObjects(string.Format("SELECT mit_lids_functions.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_volume_detail LEFT JOIN mit_products ON mit_alt_volume_detail.products_id = mit_products.id WHERE (mit_alt_volume_detail.updated_at is null AND mit_alt_volume_detail.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mit_alt_volume_detail.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            var tableFunctionDetailLid = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_lids_functions WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableFunctionDetailLid != null)
            {
                Trace.WriteLine($"Start Sync Function Detail Lid {tableFunctionDetailLid.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableFunctionDetailLid.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync FunctionDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE FunctionDetails WHERE RefID = {0} AND IsDeleted = 0 AND Type = 'Lid'", row["ID"]));

                            var FunctionDetail = new FunctionDetail();

                            FunctionDetail.RefID = Convert.ToInt64(row["id"]);
                            FunctionDetail.FunctionsID = row["mit_functions_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_functions_id"]);
                            FunctionDetail.ObjectID = row["mit_lids_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_lids_id"]);
                            FunctionDetail.Type = "Lid";
                            FunctionDetailRepository.Insert(FunctionDetail);

                            Trace.WriteLine($"Success syncing FunctionDetailID : {Convert.ToInt64(row["ID"])}");
                            Utility.DeleteDiffData("mit_lids_functions", "FunctionDetails", "AND Type = 'Lid'");

                            Trace.WriteLine($"Delete Diff Data completed successfully.");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing FunctionDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Function Detail Lid completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Function....");
        }

        public static Int64 Insert(FunctionDetail FunctionDetail)
        {
            try
            {
                var sql = @"INSERT INTO [FunctionDetails] (FunctionsID, ObjectID, Type, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@FunctionsID, @ObjectID, @Type, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                FunctionDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, FunctionDetail);

                ///888 from integration
                //SystemLogRepository.Insert("FunctionDetail", FunctionDetail.ID, 888, FunctionDetail);
                return FunctionDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(FunctionDetail FunctionDetail)
        {
            try
            {
                var sql = @"UPDATE [FunctionDetails] SET
                            FunctionsID = @FunctionsID,
                            ObjectID = @ObjectID,
                            Type = @Type,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, FunctionDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("FunctionDetail", FunctionDetail.ID, 888, FunctionDetail);
                return FunctionDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
