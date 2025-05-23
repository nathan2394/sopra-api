using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using Sopra.Helpers;

namespace Sopra.Services
{
    public class StockWMSRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync StockWMSRepository....");

            var tableStock = Utility.SQLGetObjects("SELECT * FROM vSyncStock", Utility.WMSDBConnection);
            if (tableStock == null || tableStock.Rows.Count <= 0) return;

            //Reset Data OS
            Utility.ExecuteNonQueryMySQL($"update mit_calculators set data_outstanding = 0, data_stock = 0");
            Utility.ExecuteNonQueryMySQL($"update mit_products set stock = 0");

            Utility.ExecuteNonQuery($"update Calculators set Stock = 0");
            Utility.ExecuteNonQuery($"update ProductDetails2 set Stock = 0");


            foreach (DataRow row in tableStock.Rows)
            {
                try
                {
                    Trace.WriteLine($"Sync Stock Item {row["ItemCode"]} - {row["Stock"]}");
                    //MYSQL
                    Utility.ExecuteNonQueryMySQL($"update mit_calculators a join mit_products b on a.id = b.mit_calculators_id set a.data_stock  = {Math.Round(Convert.ToDecimal(row["Stock"]), 0)} where b.wms_code  = '{row["ItemCode"]}'");
                    Utility.ExecuteNonQueryMySQL($"update mit_products set stock = {Math.Round(Convert.ToDecimal(row["Balance"]), 0)} where wms_code  = '{row["ItemCode"]}'");
                    //Update OS Order
                    Utility.ExecuteNonQueryMySQL($"update mit_calculators a join mit_products b on a.id = b.mit_calculators_id set a.data_outstanding  = {Math.Round(Convert.ToDecimal(row["Outstanding"]), 0)} where b.wms_code  = '{row["ItemCode"]}'");

                    //SQL
                    Utility.ExecuteNonQuery($"update Calculators set Stock  = {Math.Round(Convert.ToDecimal(row["Stock"]), 0)} where WmsCode  = '{row["ItemCode"]}'", Utility.SQLDBConnection);
                    Utility.ExecuteNonQuery($"update ProductDetails2 set stock = {Math.Round(Convert.ToDecimal(row["Stock"]), 0)} where WmsCode  = '{row["ItemCode"]}'", Utility.SQLDBConnection);

                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Error : {e.Message}");
                    Thread.Sleep(100);
                }
            }

        }

    }
}
