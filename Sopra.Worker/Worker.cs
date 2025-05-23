using Microsoft.Extensions.Configuration;

using System.Diagnostics;

using Sopra.Helpers;
using Sopra.Services;
namespace Sopra.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IConfigurationRoot config;
        public Worker(ILogger<Worker> logger)
        {
            Trace.Listeners.Add(new MyTraceListener(this.OutputTraceMessage));

            this.config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            _logger = logger;
        }

        private void OutputTraceMessage(string message)
        {
            try { _logger.LogInformation(string.Format("[{0}] {1}", DateTimeOffset.Now, message)); }
            catch (System.StackOverflowException) { }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                IntegrationService.Run(this.config);
                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}

