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
    public class DepositRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Deposit....");
            

            var tableDeposit = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_deposit WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableDeposit != null)
            {
                Trace.WriteLine($"Start Deposit {tableDeposit.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableDeposit.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync DepositID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Deposits WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Deposit = new Deposit();

                            Deposit.RefID = Convert.ToInt64(row["id"]);
                            Deposit.CustomersID = row["customers_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["customers_id"]);
                            Deposit.TotalAmount = row["total_amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_amount"]);
                            DepositRepository.Insert(Deposit);

                            Trace.WriteLine($"Success syncing DepositID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing DepositID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Alt Neck completed successfully.");
                    //Utility.DeleteDiffData("mit_deposit", "Deposits");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Deposit....");
        }

        public static Int64 Insert(Deposit Deposit)
        {
            try
            {
                var sql = @"INSERT INTO [Deposits] (RefID, CustomersID, TotalAmount, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @CustomersID, @TotalAmount, @DateIn, @UserIn, @IsDeleted) ";
                Deposit.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Deposit);

                ///888 from integration
                //SystemLogRepository.Insert("Deposit", Deposit.ID, 888, Deposit);
                return Deposit.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Deposit Deposit)
        {
            try
            {
                var sql = @"UPDATE [Deposits] SET
                            CustomersID = @CustomersID,
                            TotalAmount = @TotalAmount,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Deposit, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Deposit", Deposit.ID, 888, Deposit);
                return Deposit.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
