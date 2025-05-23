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
    public class NotificationRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Notification Detail....");


            var tableNotification = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_notifications WHERE created_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.TransactionSyncDate), Utility.MySQLDBConnection);
            if (tableNotification != null)
            {
                Trace.WriteLine($"Start Sync Notification Detail {tableNotification.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableNotification.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync NotificationID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Notifications WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Notification = new Notification();

                            Notification.RefID = Convert.ToInt64(row["id"]);
                            Notification.UserID = row["mit_users_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["mit_users_id"]);
                            Notification.Content = row["content"] == DBNull.Value ? null : Convert.ToString(row["content"]);
                            Notification.ContentID = row["contentID"] == DBNull.Value ? null : Convert.ToString(row["contentID"]);
                            Notification.URL = row["url"] == DBNull.Value ? null : Convert.ToString(row["url"]);
                            Notification.IsRead = row["is_read"] == DBNull.Value ? false : Convert.ToBoolean(row["is_read"]);
                            NotificationRepository.Insert(Notification);

                            Trace.WriteLine($"Success syncing NotificationID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing NotificationID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Notification completed successfully.");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Notification....");
        }

        public static Int64 Insert(Notification Notification)
        {
            try
            {
                var sql = @"INSERT INTO [Notifications] (Content,URL,IsRead,UserID, RefID,ContentID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Content,@URL,@IsRead,@UserID ,@RefID,@ContentID, @DateIn, @UserIn, @IsDeleted) ";
                Notification.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Notification);

                ///888 from integration
                //SystemLogRepository.Insert("Notification", Notification.ID, 888, Notification);
                return Notification.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
