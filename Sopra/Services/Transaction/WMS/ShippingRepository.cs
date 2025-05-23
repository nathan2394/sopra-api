using System;
using System.Diagnostics;
using System.Threading;
using System.Data;
using Dapper;

using Sopra.Helpers;
using MySqlConnector;

namespace Sopra.Services
{
    public class ShippingRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Shipping....");
            //COMBINE SOPRA & TRASS
            var tableShippingWMS = Utility.SQLGetObjects(
                @"
                    SELECT A.Id, A.VoucherNo, A.TransDate, C.ReferenceNo, D.Code, SUM(B.Qty * D.QtyPerPacking) AS Delivered
                    FROM Shippings A
                        INNER JOIN ShippingDetails B ON A.Id = B.ShippingId
                        INNER JOIN Orders C ON A.OrderId = C.Id
                        INNER JOIN Items D ON B.ItemId = D.Id
                    WHERE C.ReferenceNo LIKE 'SOPRA/SC%'
                        AND A.DateIn > CAST(GETDATE() AS DATE)
                        AND A.[Status] = 'Y'
                    GROUP BY A.VoucherNo, A.TransDate, C.ReferenceNo, D.Code, A.Id
                    UNION ALL
                    SELECT A.Id, A.VoucherNo, A.TransDate, C.ReferenceNo, D.Code, SUM(B.Qty * D.QtyPerPacking) AS Delivered
                    FROM WMS_TRASS..Shippings A
                        INNER JOIN WMS_TRASS..ShippingDetails B ON A.Id = B.ShippingId
                        INNER JOIN WMS_TRASS..Orders C ON A.OrderId = C.Id
                        INNER JOIN WMS_TRASS..Items D ON B.ItemId = D.Id
                    WHERE C.ReferenceNo LIKE 'TRASS/SC%'
                        AND A.DateIn > CAST(GETDATE() AS DATE)
                        AND A.[Status] = 'Y'
                    GROUP BY A.VoucherNo, A.TransDate, C.ReferenceNo, D.Code, A.Id", Utility.WMSDBConnection);
            if (tableShippingWMS != null && tableShippingWMS.Rows.Count > 0)
            {
                foreach (DataRow row in tableShippingWMS.Rows)
                {
                    int mit_order_bottle_id = 0;
                    int product_id = 0;

                    // Find ID Based On MYSQL
                    // Continue if not exist
                    if (row["ReferenceNo"] == null || row["ReferenceNo"] == DBNull.Value) continue;

                    var voucherno_order = row["ReferenceNo"].ToString();
                    Trace.WriteLine($"Sync VoucherNo Ecom : {voucherno_order}");

                    var tblName = "mit_orders_bottle";
                    if (voucherno_order.Contains("TRASS")) continue; //tblName = "mit_orders_bottle_trass";

                    var obj = Utility.MySqlFindObject("id", tblName, $"voucher_no  = '{voucherno_order}' and order_status = 'ACTIVE'", "", "", "", Utility.MySQLDBConnection);
                    if (obj == null || obj == DBNull.Value) continue;
                    else mit_order_bottle_id = Convert.ToInt16(obj);

                    obj = Utility.MySqlFindObject("id", "mit_products", $"wms_code  = '{row["Code"]}'", "", "", "", Utility.MySQLDBConnection);
                    if (obj == null || obj == DBNull.Value) continue;
                    else product_id = Convert.ToInt16(obj);

                    #region  InsertToSopraMySQL
                    //DELETE based on ReferenceNo
                    //INSERT to mySQL
                    try
                    {
                        Utility.ExecuteNonQueryMySQL(string.Format("delete from mit_order_shipping_detail where spk_no  = '{0}'", row["VoucherNo"]));

                        var msql =
                            @"
                                insert into mit_order_shipping_detail (spk_no, transdate, mit_order_bottle_id, product_id, qty, created_at)
                                values (@spk_no, @transdate, @mit_order_bottle_id, @product_id, @qty, @created_at)
                            ";

                        if (Utility.MySQLDBConnection.State != ConnectionState.Open) Utility.MySQLDBConnection.Open();

                        var msqlcmd = new MySqlCommand(msql, Utility.MySQLDBConnection);

                        msqlcmd.Parameters.AddWithValue("@spk_no", row["VoucherNo"]);
                        msqlcmd.Parameters.AddWithValue("@transdate", row["TransDate"]);
                        msqlcmd.Parameters.AddWithValue("@mit_order_bottle_id", mit_order_bottle_id);
                        msqlcmd.Parameters.AddWithValue("@product_id", product_id);
                        msqlcmd.Parameters.AddWithValue("@qty", row["Delivered"]);
                        msqlcmd.Parameters.AddWithValue("@created_at", row["TransDate"]);

                        msqlcmd.ExecuteNonQuery();
                        msqlcmd.Parameters.Clear();

                        Trace.WriteLine($"Success syncing OrderNo : {voucherno_order}");
                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error syncing OrderNo: {voucherno_order} - {ex.Message}");
                        Thread.Sleep(100);
                    }
                    #endregion
                }
            }

            Trace.WriteLine($"Synchronization Order Detail Reguler completed successfully.");
            Thread.Sleep(100);
            Trace.WriteLine("Finished Sync Shipping....");
        }
    }
}
