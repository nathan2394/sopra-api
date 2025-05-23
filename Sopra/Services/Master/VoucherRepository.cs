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
    public class VoucherRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Voucher....");
            

            var tableVoucher = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_vouchers WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableVoucher != null)
            {
                Trace.WriteLine($"Start Sync Voucher {tableVoucher.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableVoucher.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync VoucherID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Vouchers WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Voucher = new Voucher();

                            Voucher.RefID = Convert.ToInt64(row["id"]);
                            Voucher.VoucherNo = row["voucher_no"] == DBNull.Value ? null : row["voucher_no"].ToString();
                            Voucher.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            Voucher.ExpiredDate = row["expired_date"] == DBNull.Value ? null : Convert.ToDateTime(row["expired_date"]);
							Voucher.OrdersID = row["orders_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["orders_id"]);
							Voucher.Disc = row["disc"] == DBNull.Value ? 0 : Convert.ToInt64(row["disc"]);
							Voucher.VoucherUsage = row["voucher_usage"] == DBNull.Value ? 0 : Convert.ToInt64(row["voucher_usage"]);
							Voucher.MinOrder = row["min_order"] == DBNull.Value ? 0 : Convert.ToDecimal(row["min_order"]);
							Voucher.Status = row["status"] == DBNull.Value ? null : row["status"].ToString();
							Voucher.FlagOrder = row["flag_order"] == DBNull.Value ? null : row["flag_order"].ToString();
                            VoucherRepository.Insert(Voucher);

                            Trace.WriteLine($"Success syncing VoucherID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing VoucherID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Voucher completed successfully.");
                    //Utility.DeleteDiffData("mit_vouchers", "Vouchers");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Voucher....");
        }

        public static Int64 Insert(Voucher Voucher)
        {
            try
            {
                var sql = @"INSERT INTO [Vouchers] (RefID, VoucherNo, Amount, ExpiredDate, OrdersID, Disc, VoucherUsage, MinOrder, Status, FlagOrder, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES ( @RefID, @VoucherNo, @Amount, @ExpiredDate, @OrdersID, @Disc, @VoucherUsage, @MinOrder, @Status, @FlagOrder, @DateIn, @UserIn, @IsDeleted) ";
                Voucher.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Voucher);

                ///888 from integration
                //SystemLogRepository.Insert("Voucher", Voucher.ID, 888, Voucher);
                return Voucher.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Voucher Voucher)
        {
            try
            {
                var sql = @"UPDATE [Vouchers] SET
                            VoucherNo = @VoucherNo,
                            Amount = @Amount,
                            ExpiredDate = @ExpiredDate,
                            OrdersID = @OrdersID,
                            Disc = @Disc,
                            VoucherUsage = @VoucherUsage,
                            MinOrder = @MinOrder,
                            Status = @Status,
                            FlagOrder = @FlagOrder,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Voucher, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Voucher", Voucher.ID, 888, Voucher);
                return Voucher.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
