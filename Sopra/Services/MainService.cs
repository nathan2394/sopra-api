using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace Sopra.Services
{
    public class MainService
    {
        private bool isStarted;
        protected bool isDone;
        protected AutoResetEvent eventSvc;

        protected Thread tSynchronized;

        private IConfigurationRoot config;

        public MainService(IConfigurationRoot config)
        {
            this.config = config;
        }
        public void Start()
        {
            if (this.isStarted)
                throw new Exception("Service Already Started.");

            this.tSynchronized = new Thread(Synchronized);

            this.eventSvc = new AutoResetEvent(false);
            this.isDone = false;
            this.isStarted = true;

            //hold auto sync at server 
            tSynchronized.Start();

            Trace.WriteLine("Service started.", "Start");
        }

        public void Stop()
        {
            if (!this.isStarted)
                throw new Exception("Service Not Started.");

            this.isDone = true;

            // Clean Up
            this.tSynchronized = null;

            this.isStarted = false;
            Trace.WriteLine("Service stopped.", "Stop");
        }

        private void Synchronized()
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
                            IntegrationService.Run(this.config);
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
