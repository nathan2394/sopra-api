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
    public class PaymentRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Payment....");
            var deleteData = Utility.SQLDBConnection.QueryFirstOrDefault<Payment>(string.Format("TRUNCATE TABLE Payments"), transaction: null);
            //GET DATA FROM MYSQL
            var tablePaymentBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_payments_bottle WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tablePaymentBottle != null)
            {
                Trace.WriteLine($"Start Sync Payment Bottle {tablePaymentBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tablePaymentBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync PaymentID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<Payment>(string.Format("SELECT * FROM Payments WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var Payment = new Payment();
                            if (obj != null) Payment = obj;

                            Payment.RefID = Convert.ToInt64(row["id"]);
                            Payment.PaymentNo = row["payment_no"] == DBNull.Value ? null : row["payment_no"].ToString();
                            Payment.Type = "Bottle";
                            Payment.TransDate = row["transdate"] == DBNull.Value ? null : Convert.ToDateTime(row["transdate"]);
                            Payment.CustomersID = row["customers_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["customers_id"]);
                            Payment.InvoicesID = row["invoice_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["invoice_id"]);
                            Payment.Netto = row["netto"] == DBNull.Value ? 0 : Convert.ToDecimal(row["netto"]);
                            Payment.BankRef = row["bank_ref"] == DBNull.Value ? null : row["bank_ref"].ToString();
                            Payment.BankTime = row["bank_time"] == DBNull.Value ? null : Convert.ToDateTime(row["bank_time"]);
                            Payment.AmtReceive = row["amt_receive"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amt_receive"]);
                            Payment.Status = row["payment_status"] == DBNull.Value ? null : row["payment_status"].ToString();
                            Payment.ReasonsID = row["reason_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["reason_id"]);
                            Payment.Note = row["notes"] == DBNull.Value ? null : row["notes"].ToString();
                            Payment.Username = row["username"] == DBNull.Value ? null : row["username"].ToString();
                            Payment.ReasonsID = row["reason_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["reason_id"]);
                            Payment.UsernameCancel = row["username_cancel"] == DBNull.Value ? null : row["username_cancel"].ToString();
                            Payment.CompaniesID = row["company_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["company_id"]);

                            if (obj == null)
                            {
                                // INSERT
                                Payment.UserIn = 888;
                                PaymentRepository.Insert(Payment);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS Payment
                                Payment.UserUp = 888;
                                PaymentRepository.Update(Payment);
                            }

                            Trace.WriteLine($"Success syncing PaymentID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing PaymentID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Payment Bottle completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Payment....");
        }

        public static Int64 Insert(Payment Payment)
        {
            try
            {
                var sql = @"INSERT INTO [Payments] (RefID, PaymentNo, Type, TransDate, CustomersID, InvoicesID, Netto, BankRef, BankTime, AmtReceive, Status, ReasonsID, Note, Username, UsernameCancel, CompaniesID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @PaymentNo, @Type, @TransDate, @CustomersID, @InvoicesID, @Netto, @BankRef, @BankTime, @AmtReceive, @Status, @ReasonsID, @Note, @Username, @UsernameCancel, @CompaniesID, @DateIn, @UserIn, @IsDeleted) ";
                Payment.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Payment);

                ///888 from integration
                //SystemLogRepository.Insert("Payment", Payment.ID, 888, Payment);
                return Payment.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Payment Payment)
        {
            try
            {
                var sql = @"UPDATE [Payments] SET
                            PaymentNo = @PaymentNo,
                            Type = @Type,
                            TransDate = @TransDate,
                            CustomersID = @CustomersID,
                            InvoicesID = @InvoicesID,
                            Netto = @Netto,
                            BankRef = @BankRef,
                            BankTime = @BankTime,
                            AmtReceive = @AmtReceive,
                            Status = @Status,
                            ReasonsID = @ReasonsID,
                            Note = @Note,
                            Username = @Username,
                            UsernameCancel = @UsernameCancel,
                            CompaniesID = @CompaniesID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Payment, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Payment", Payment.ID, 888, Payment);
                return Payment.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
