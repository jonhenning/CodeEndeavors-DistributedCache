using CodeEndeavors.Extensions;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.SampleSignalRServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientCount = 2;
            var url = "http://localhost:8080";
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Web Server is running.");
                Console.WriteLine("How many clients do you wish to spawn?");
                if (!int.TryParse(Console.ReadLine(), out clientCount))
                    clientCount = 2;
                

                Console.WriteLine("Starting {0} clients...", clientCount);
                for (var i = 0; i < clientCount; i++)
                {
                    startClient(url, "SampleClient" + (i + 1));
                }

                Console.WriteLine("Press any key to quit.");
                Console.ReadLine();
            }
        }

        private static void startClient(string url, string name)
        {
            var process = new ProcessStartInfo() { FileName = Environment.CurrentDirectory.PathCombine(@"..\..\CodeEndeavors.Distributed.Cache.SampleClient\bin\CodeEndeavors.Distributed.Cache.SampleClient.exe"), Arguments = url + " " + name };
            System.Diagnostics.Process.Start(process);

        }
    }
}
