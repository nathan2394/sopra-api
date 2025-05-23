using Sopra.Entities;
using Sopra.Helpers;
using System;
using System.Data;
using System.Diagnostics;
using Dapper;
using System.Threading;

namespace Sopra.Services.Master
{
    public class UserDealerRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync User User Dealer....");
            

            var tableUserDealer = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_users_dealer WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableUserDealer != null)
            {
                Trace.WriteLine($"Start Sync Alt Neck {tableUserDealer.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableUserDealer.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync UserDealerID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE UserDealers WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var UserDealer = new UserDealer();

                            UserDealer.RefID = Convert.ToInt64(row["id"]);
                            UserDealer.UserId = row["mit_users_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["mit_users_id"]);
                            UserDealer.DealerId = row["mit_dealer_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["mit_dealer_id"]);
                            UserDealer.StartDate = row["start_date"] == DBNull.Value ? null : Convert.ToDateTime(row["start_date"]);
                            UserDealer.EndDate = row["end_date"] == DBNull.Value ? null : Convert.ToDateTime(row["end_date"]);
                            UserDealerRepository.Insert(UserDealer);

                            Trace.WriteLine($"Success syncing UserDealerID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing UserDealerID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization User Dealer completed successfully.");
                    //Utility.DeleteDiffData("mit_users_dealer", "UserDealers");

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

        public static Int64 Insert(UserDealer UserDealer)
        {
            try
            {
                var sql = @"INSERT INTO [UserDealers] (UserId,DealerId,StartDate,EndDate, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@UserId,@DealerId,@StartDate,@EndDate, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                UserDealer.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, UserDealer);

                ///888 from integration
                //SystemLogRepository.Insert("UserDealer", UserDealer.ID, 888, UserDealer);
                return UserDealer.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(UserDealer UserDealer)
        {
            try
            {
                var sql = @"UPDATE [UserDealers] SET
                            UserId = @UserId,
                            DealerId = @DealerId,
                            StartDate = @StartDate,
                            EndDate= @EndDate,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, UserDealer, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("UserDealer", UserDealer.ID, 888, UserDealer);
                return UserDealer.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
