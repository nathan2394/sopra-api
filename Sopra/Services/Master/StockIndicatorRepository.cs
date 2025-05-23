using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;

namespace SOPRA.Services
{
    public class StockIndicatorRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Stock Indicator....");
            
            ///GET DATA FROM MYSQL
            var tableStockIndicator = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_stock_indicator WHERE (updated_at IS NULL AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableStockIndicator != null)
            {
                Trace.WriteLine($"Start Sync Stock Indicator {tableStockIndicator.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableStockIndicator.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync StockIndicatorID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE StockIndicators WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var StockIndicator = new StockIndicator();
                            StockIndicator.RefID = Convert.ToInt64(row["id"]);
                            StockIndicator.Name = row["name"].ToString();
                            StockIndicator.MinQty = Convert.ToDecimal(row["min_qty"]);
                            StockIndicator.MaxQty = Convert.ToDecimal(row["max_qty"]);
                            StockIndicator.UserIn = 888;
                            StockIndicatorRepository.Insert(StockIndicator);

                            Trace.WriteLine($"Success syncing StockIndicatorID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing StockIndicatorID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization StockIndicatorID completed successfully.");

                    //Utility.DeleteDiffData("mit_stock_indicator","StockIndicators");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);

                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Stock Indicator....");
        }

        public static Int64 Insert(StockIndicator StockIndicator)
        {
            try
            {
                var sql = @"INSERT INTO [StockIndicators]  (RefID, Name, MinQty, MaxQty, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES  (@RefID, @Name, @MinQty, @MaxQty, @DateIn, @UserIn, @IsDeleted) ";
                StockIndicator.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, StockIndicator);

                ///888 from integration
                //SystemLogRepository.Insert("Stock Indicator", StockIndicator.ID, 888, StockIndicator);
                return StockIndicator.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
