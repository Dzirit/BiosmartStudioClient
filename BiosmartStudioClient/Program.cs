using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BiosmarStudioClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                 .Build();
            var bs = new BiosmartManager(config);
            var templates = await bs.GetTemplates();
            
            Console.ReadLine();
        }
    }
}
