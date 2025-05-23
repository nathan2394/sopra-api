using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Linq;
using System.Text;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;

namespace Sopra.Services
{
    public class DiscountRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Discount....");

            ///GET DATA FROM MYSQL
            var tableDiscount = Utility.MySqlGetObjects(string.Format("select id,created_at ,updated_at,discs,amount,amount_max from mit_discounts  union all select id,created_at ,updated_at,disc as discs,quantity as amount,0 as amount_max from mit_discounts_thermo ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableDiscount != null)
            {
                Trace.WriteLine($"Start Sync Discount {tableDiscount.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableDiscount.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync DiscountID : {Convert.ToInt64(row["ID"])}");
                            var type = row["amount_max"] == DBNull.Value ? null : Convert.ToInt64(row["amount_max"]) == 0 ? "thermo" : "bottle";
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Discounts WHERE RefID = {0} AND IsDeleted = 0 AND Type = '{1}'", row["ID"],type));

                            var Discount = new Discount();

                            Discount.RefID = Convert.ToInt64(row["id"]);
                            Discount.Disc = row["discs"] == DBNull.Value ? 0 : Convert.ToDecimal(row["discs"]);
                            Discount.Amount= row["amount"] == DBNull.Value ? 0 : Convert.ToInt64(row["amount"]);
                            Discount.AmountMax = row["amount_max"] == DBNull.Value ? 0 : Convert.ToInt64(row["amount_max"]);
                            Discount.Type = type;
                            Discount.UserIn = 888;
                            DiscountRepository.Insert(Discount);

                            Trace.WriteLine($"Success syncing DiscountID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing DiscountID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Discount completed successfully.");

                    //Utility.DeleteDiffData("mit_discounts", "Discounts", "AND Type = 'bottle'");
                    //Utility.DeleteDiffData("mit_discounts_thermo", "Discounts","AND Type = 'thermo'");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Discount....");
        }

        public static Int64 Insert(Discount Discount)
        {
            try
            {
                var sql = @"INSERT INTO [Discounts]  (RefID, Disc,Amount,AmountMax,Type, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES  (@RefID, @Disc,@Amount,@AmountMax,@Type,@DateIn, @UserIn, @IsDeleted) ";
                Discount.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Discount);

                ///888 from integration
                //SystemLogRepository.Insert("Discount", Discount.ID, 888, Discount);
                return Discount.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
