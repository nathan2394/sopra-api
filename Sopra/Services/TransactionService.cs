using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sopra.Helpers;

namespace Sopra.Services
{
    public class TransactionService
    {
        private bool isStarted;
        private readonly EFContext context;
        private readonly HttpClient httpClient;
        protected bool isDone;
        protected AutoResetEvent eventSvc;

        protected Thread tSynchronized;

        private IConfigurationRoot config;

        public TransactionService(IConfigurationRoot config)
        {
            this.config = config;

            // Setup DbContextOptions
            var optionsBuilder = new DbContextOptionsBuilder<EFContext>();
            optionsBuilder.UseSqlServer(config.GetSection("AppSettings")["ConnectionString"]);
            optionsBuilder.EnableSensitiveDataLogging();

            // Initialize the readonly field in the constructor
            this.context = new EFContext(optionsBuilder.Options);
            this.httpClient = new HttpClient();
        }
        public async Task Start()
        {
            if (this.isStarted)
                throw new Exception("Service Already Started.");

            //this.tSynchronized = Task.Run(Synchronized);
            var tSynchronized = Task.Run(async () => await Synchronized());
            //this.tSynchronized = tSynchronized;

            this.eventSvc = new AutoResetEvent(false);
            this.isDone = false;
            this.isStarted = true;

            //hold auto sync at server 
            //tSynchronized.Start();
            await tSynchronized;
            Trace.WriteLine("Service started.", "Start");
        }

        private async Task Synchronized()
        {
            var nextRunTime = DateTime.Today;
            try
            {
                while (!this.isDone)
                {
                    if (nextRunTime <= DateTime.Now)
                    {
                        try
                        {
                            await Sopra.Services.Transaction.MainService.Run(this.config, this.context,this.httpClient);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(string.Format("Script Err : {0}\r\nStack Trace:{1}", ex.Message, ex.StackTrace), "Scheduller");
                        }
                        finally
                        {
                            nextRunTime = DateTime.Now.AddMinutes(5);
                            Trace.WriteLine(string.Format("Next Run Time : {0:dd MMM yyy HH:mm:ss}", nextRunTime), "Scheduller");
                            Environment.Exit(0);
                        }
                    }
                    this.eventSvc.WaitOne(300000, false);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
