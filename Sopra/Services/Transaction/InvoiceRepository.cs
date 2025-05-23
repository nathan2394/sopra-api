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
using System.Globalization;

namespace Sopra.Services
{
    public class InvoiceRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Invoice....");
            var deleteData = Utility.SQLDBConnection.QueryFirstOrDefault<Invoice>(string.Format("TRUNCATE TABLE Invoices"), transaction: null);
            //GET DATA FROM MYSQL
            var tableInvoiceBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_invoice_bottle WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableInvoiceBottle != null)
            {
                Trace.WriteLine($"Start Sync Invoice {tableInvoiceBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableInvoiceBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync InvoiceID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<Invoice>(string.Format("SELECT * FROM Invoices WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var Invoice = new Invoice();
                            if (obj != null) Invoice = obj;

                            Invoice.RefID = Convert.ToInt64(row["id"]);
                            Invoice.OrdersID = row["orders_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["orders_id"]);
                            Invoice.PaymentMethod = row["payment_method"] == DBNull.Value ? 0 : Convert.ToInt64(row["payment_method"]);
                            Invoice.InvoiceNo = row["invoice_no"] == DBNull.Value ? null : row["invoice_no"].ToString();
                            Invoice.Type = "Bottle";
                            Invoice.Netto = row["netto"] == DBNull.Value ? 0 : Convert.ToDecimal(row["netto"]);
                            Invoice.CustomersID = row["customers_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["customers_id"]);
                            //Invoice.TransDate = row["transdate"] == DBNull.Value ? null : Convert.ToDateTime(row["transdate"]);
                            Invoice.TransDate = row["transdate"] == DBNull.Value ? (DateTime?)null : DateTime.TryParseExact(row["transdate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ? parsedDate : (DateTime?)null;
                            Invoice.Status = row["inv_status"] == DBNull.Value ? null : row["inv_status"].ToString();
                            Invoice.VANum = row["va_num"] == DBNull.Value ? null : row["va_num"].ToString();
                            Invoice.CustNum = row["cust_num"] == DBNull.Value ? null : row["cust_num"].ToString();
                            Invoice.FlagInv = row["flag_inv"] == DBNull.Value ? 0 : Convert.ToInt64(row["flag_inv"]);
                            Invoice.ReasonsID = row["reason_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["reason_id"]);
                            Invoice.Note = row["notes"] == DBNull.Value ? null : row["notes"].ToString();
                            Invoice.Refund = row["refund"] == DBNull.Value ? 0 : Convert.ToDecimal(row["refund"]);
                            Invoice.Bill = row["bill"] == DBNull.Value ? 0 : Convert.ToDecimal(row["bill"]);
                            Invoice.PICInv = row["pic_inv"] == DBNull.Value ? 0 : Convert.ToInt64(row["pic_inv"]);
                            Invoice.BankName = row["bank_name"] == DBNull.Value ? null : row["bank_name"].ToString();
                            Invoice.AccountNumber = row["account_number"] == DBNull.Value ? null : row["account_number"].ToString();
                            Invoice.AccountName = row["account_name"] == DBNull.Value ? null : row["account_name"].ToString();
                            Invoice.TransferDate = row["transfer_date"] == DBNull.Value ? null : Convert.ToDateTime(row["transfer_date"]);
                            Invoice.BankRef = row["bank_ref"] == DBNull.Value ? null : row["bank_ref"].ToString();
                            Invoice.TransferAmount = row["transfer_amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["transfer_amount"]);
                            Invoice.Username = row["username"] == DBNull.Value ? null : row["username"].ToString();
                            Invoice.UsernameCancel = row["username_cancel"] == DBNull.Value ? null : row["username_cancel"].ToString();
                            Invoice.XenditID = row["xendit_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["xendit_id"]);
                            Invoice.XenditBank = row["xendit_bank"] == DBNull.Value ? null : row["xendit_bank"].ToString();
                            Invoice.PDFFile = row["pdf_file"] == DBNull.Value ? null : row["pdf_file"].ToString();
                            Invoice.DueDate = row["due_date"] == DBNull.Value ? null : Convert.ToDateTime(row["due_date"]);
                            Invoice.CompaniesID = row["company_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["company_id"]);
                            Invoice.SentWaCounter = row["sent_wa_counter"] == DBNull.Value ? 0 : Convert.ToInt64(row["sent_wa_counter"]);
                            Invoice.WASentTime = row["wa_sent_time"] == DBNull.Value ? null : Convert.ToDateTime(row["wa_sent_time"]);
                            Invoice.FutureDateStatus = row["future_date_status"] == DBNull.Value ? false : Convert.ToInt32(row["future_date_status"]) == 0 ? false : true;
                            Invoice.CreditStatus = row["credit_status"] == DBNull.Value ? false : Convert.ToInt32(row["credit_status"]) == 0 ? false : true;

                            if (obj == null)
                            {
                                // INSERT
                                Invoice.UserIn = 888;
                                InvoiceRepository.Insert(Invoice);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS Invoice
                                Invoice.UserUp = 888;
                                InvoiceRepository.Update(Invoice);
                            }

                            Trace.WriteLine($"Success syncing InvoiceID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing InvoiceID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Invoice completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Invoice....");
        }

        public static Int64 Insert(Invoice Invoice)
        {
            try
            {
                var sql = @"INSERT INTO [Invoices] (RefID, OrdersID, PaymentMethod, InvoiceNo, Type, Netto, CustomersID, TransDate, Status, VANum, CustNum, FlagInv, ReasonsID, Note, Refund, Bill, PICInv, BankName, AccountNumber, TransferDate, BankRef, TransferAmount, Username, UsernameCancel, XenditID, XenditBank, PDFFile, DueDate, CompaniesID, SentWaCounter, WASentTime, FutureDateStatus, CreditStatus, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @OrdersID, @PaymentMethod, @InvoiceNo, @Type, @Netto, @CustomersID, @TransDate, @Status, @VANum, @CustNum, @FlagInv, @ReasonsID, @Note, @Refund, @Bill, @PICInv, @BankName, @AccountNumber, @TransferDate, @BankRef, @TransferAmount, @Username, @UsernameCancel, @XenditID, @XenditBank, @PDFFile, @DueDate, @CompaniesID, @SentWaCounter, @WASentTime, @FutureDateStatus, @CreditStatus, @DateIn, @UserIn, @IsDeleted) ";
                Invoice.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Invoice);

                ///888 from integration
                //SystemLogRepository.Insert("Invoice", Invoice.ID, 888, Invoice);
                return Invoice.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Invoice Invoice)
        {
            try
            {
                var sql = @"UPDATE [Invoices] SET
                            OrdersID = @OrdersID,
                            PaymentMethod = @PaymentMethod,
                            InvoiceNo = @InvoiceNo,
                            Type = @Type,
                            Netto = @Netto,
                            CustomersID = @CustomersID,
                            TransDate = @TransDate,
                            Status = @Status,
                            VANum = @VANum,
                            CustNum = @CustNum,
                            FlagInv = @FlagInv,
                            ReasonsID = @ReasonsID,
                            Note = @Note,
                            Refund = @Refund,
                            Bill = @Bill,
                            PICInv = @PICInv,
                            BankName = @BankName,
                            AccountNumber = @AccountNumber,
                            AccountName = @AccountName,
                            TransferDate = @TransferDate,
                            BankRef = @BankRef,
                            TransferAmount = @TransferAmount,
                            Username = @Username,
                            UsernameCancel = @UsernameCancel,
                            XenditID = @XenditID,
                            XenditBank = @XenditBank,
                            PDFFile = @PDFFile,
                            DueDate = @DueDate,
                            CompaniesID = @CompaniesID,
                            SentWaCounter = @SentWaCounter,
                            WASentTime = @WASentTime,
                            FutureDateStatus = @FutureDateStatus,
                            CreditStatus = @CreditStatus,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Invoice, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Invoice", Invoice.ID, 888, Invoice);
                return Invoice.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
