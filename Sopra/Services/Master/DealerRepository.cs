using Sopra.Entities;
using Sopra.Helpers;
using System;
using System.Data;
using System.Diagnostics;
using Dapper;
using System.Threading;

namespace Sopra.Services.Master
{
    public class DealerRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Dealer....");
            

            var tableDealer = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_dealer WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableDealer != null)
            {
                Trace.WriteLine($"Start Sync Alt Neck {tableDealer.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableDealer.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync DealerID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Dealers WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Dealer = new Dealer();

                            Dealer.RefID = Convert.ToInt64(row["id"]);
                            Dealer.Tier = row["tier"] == DBNull.Value ? null : row["tier"].ToString();
                            Dealer.DiscBottle = row["disc"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc"]);
                            Dealer.DiscThermo = row["disc_thermo"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc_thermo"]);
                            DealerRepository.Insert(Dealer);

                            Trace.WriteLine($"Success syncing DealerID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing DealerID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Dealer completed successfully.");
                    //Utility.DeleteDiffData("mit_dealer", "Dealers");

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

        public static Int64 Insert(Dealer Dealer)
        {
            try
            {
                var sql = @"INSERT INTO [Dealers] (Tier,DiscBottle,DiscThermo, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Tier,@DiscBottle,@DiscThermo, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                Dealer.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Dealer);

                ///888 from integration
                //SystemLogRepository.Insert("Dealer", Dealer.ID, 888, Dealer);
                return Dealer.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Dealer Dealer)
        {
            try
            {
                var sql = @"UPDATE [Dealers] SET
                            Tier = @Tier ,
                            DiscBottle = @DiscBottle ,
                            DiscThermo = @DiscThermo ,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Dealer, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Dealer", Dealer.ID, 888, Dealer);
                return Dealer.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
