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
    public class AltNeckDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Alt Neck Detail....");
            

            var tableAltNeckDetail = Utility.MySqlGetObjects(string.Format("SELECT mand.*,mit_products.mit_calculators_id AS calculatorid FROM mit_alt_neck_detail mand LEFT JOIN mit_products ON mand.products_id = mit_products.id WHERE (mand.updated_at is null AND mand.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mand.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableAltNeckDetail != null)
            {
                Trace.WriteLine($"Start Sync Alt Neck Detail {tableAltNeckDetail.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableAltNeckDetail.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync AltNeckDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE AltNeckDetails WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var AltNeckDetail = new AltNeckDetail();

                            AltNeckDetail.RefID = Convert.ToInt64(row["id"]);
                            AltNeckDetail.AltNecksID = row["alt_neck_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["alt_neck_id"]);
                            AltNeckDetail.CalculatorsID = row["calculatorid"] == DBNull.Value ? 0 : Convert.ToInt64(row["calculatorid"]);
                            AltNeckDetailRepository.Insert(AltNeckDetail);

                            Trace.WriteLine($"Success syncing AltNeckDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing AltNeckDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Neck completed successfully.");
                    //Utility.DeleteDiffData("mit_alt_neck_detail", "AltNeckDetails");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
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

        public static Int64 Insert(AltNeckDetail AltNeckDetail)
        {
            try
            {
                var sql = @"INSERT INTO [AltNeckDetails] (AltNecksID, CalculatorsID, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@AltNecksID, @CalculatorsID, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                AltNeckDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, AltNeckDetail);

                ///888 from integration
                //SystemLogRepository.Insert("AltNeckDetail", AltNeckDetail.ID, 888, AltNeckDetail);
                return AltNeckDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(AltNeckDetail AltNeckDetail)
        {
            try
            {
                var sql = @"UPDATE [AltNeckDetails] SET
                            AltNecksID = @AltNecksID,
                            CalculatorsID = @CalculatorsID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, AltNeckDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("AltNeckDetail", AltNeckDetail.ID, 888, AltNeckDetail);
                return AltNeckDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
