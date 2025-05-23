using Microsoft.Extensions.Configuration;
using Sopra.Helpers;
using Sopra.Services.Master;
using SOPRA.Services;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sopra.Services.Transaction
{
    public class MainService
    {
        public static async Task Run(IConfigurationRoot config, EFContext context,HttpClient httpclient)
        {
            Trace.WriteLine("========== Transaction Service ==========");
            Trace.WriteLine("Running Sync Services....");
            Trace.WriteLine($"Time: {DateTime.Now:dd MMM yyyy HH:mm:ss}");

            Trace.WriteLine("================");
            Trace.WriteLine("Try connect to DB....");
            Utility.ConnectSQL(config["SQL:Server"], config["SQL:Database"], config["SQL:UserID"], config["SQL:Password"]);
            Trace.WriteLine("Success connect to DB....");

            Trace.WriteLine("================");
            Trace.WriteLine("Get Last Sync Date....");
            Utility.SyncDate = DateTime.Now;

            var obj = Utility.FindObject("[Value]", "Configurations", "Code = 'LastSyncDate'", "", "", "", Utility.SQLDBConnection);
            if (obj != null && obj != DBNull.Value) { }
            Utility.SyncDate = Convert.ToDateTime(obj);
            Trace.WriteLine(string.Format("Last Sync: {0:yyyy-MM-dd HH:mm:ss}", Utility.SyncDate));

            Trace.WriteLine("================");
            //await OrderSyncService.Sync(context, httpclient);
            Trace.WriteLine("================");
            Trace.WriteLine("Update Last Sync Date....");
            // Utility.ExecuteNonQuery(string.Format("UPDATE Configurations SET Value = '{0:yyyy-MM-dd HH:mm:ss}' WHERE Code = 'LastSyncDates'", DateTime.Now));
        }
    }
}
