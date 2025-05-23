using Microsoft.Extensions.Configuration;

using System;
using System.Diagnostics;

using Sopra.Helpers;
using Sopra.Services;

namespace Sopra.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new MyTraceListener());

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            var main = new MainService(config);
            main.Start();
        }
    }
}
