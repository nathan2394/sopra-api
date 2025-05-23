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
    public class SubscriptionRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Language....");
            

            var tableLanguage = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_subscription_orders WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableLanguage != null)
            {
                Trace.WriteLine($"Start Sync Subscription {tableLanguage.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableLanguage.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync SubscriptionID : {Convert.ToInt64(row["ID"])}");

                            ///CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Subscriptions WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Subscription = new Subscription();

                            Subscription.RefID = Convert.ToInt64(row["id"]);
                            Subscription.OrdersID = Convert.ToInt64(row["mit_orders_id"]);
                            Subscription.Type = row["type"].ToString();
                            Subscription.SubscriptionDate = row["subscription_date"] == DBNull.Value ? null : (DateTime)row["subscription_date"];
                            Subscription.SubscriptionType = row["subscription_type"].ToString();
                            Subscription.Status = Convert.ToInt32(row["status"]);
                            SubscriptionRepository.Insert(Subscription);

                            Trace.WriteLine($"Success syncing SubscriptionID : {Convert.ToInt64(row["ID"])}");
                           
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing SubscriptionID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Subscription completed successfully.");
                    //Utility.DeleteDiffData("mit_subscription_orders", "Subscriptions");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Subscription....");
        }

        public static Int64 Insert(Subscription Subscription)
        {
            try
            {
                var sql = @"INSERT INTO Subscriptions (RefID, OrdersID, Type, SubscriptionDate, SubscriptionType, Status, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @OrdersID, @Type, @SubscriptionDate, @SubscriptionType, @Status, @UserIn, @IsDeleted) ";
                Subscription.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Subscription);

                ///888 from integration
                //SystemLogRepository.Insert("Language", Language.ID, 888, Language);
                return Subscription.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Subscription Subscription)
        {
            try
            {
                var sql = @"UPDATE Subscriptions SET
                            OrdersID = @OrdersID,
                            Type = @Type,
                            SubscriptionDate = @SubscriptionDate, 
                            SubscriptionType = @SubscriptionType,
                            Status = @Status,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Subscription, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Subscription", Subscription.ID, 888, Subscription);
                return Subscription.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
