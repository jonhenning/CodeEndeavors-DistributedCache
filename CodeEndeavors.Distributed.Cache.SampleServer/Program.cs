using CodeEndeavors.Extensions;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.SampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientCount = 2;
            var signalRServerUrl = "http://localhost:8080";
            var redisConf = "redis.windows.conf";
            using (WebApp.Start<Startup>(signalRServerUrl))
            {
                Console.WriteLine("Select Configuration");
                Console.WriteLine("1) InMemory Cache");
                Console.WriteLine("2) InMemory Cache File Monitor");
                Console.WriteLine("3) InMemory Cache SignalR Expire");
                Console.WriteLine("4) InMemory Cache SignalR Expire FileMonitor");
                Console.WriteLine("5) InMemory Cache Redis Expire");
                Console.WriteLine("6) Redis Cache");
                var config = Console.ReadLine();

                string cacheKey = "InMemoryCache";
                string notifierKey = "none";
                string monitorKey = "none";

                if (config == "2" || config == "4")
                    monitorKey = "FileMonitor";
                if (config == "3" || config == "4")
                {
                    notifierKey = "SignalRNotifier";
                    Console.WriteLine("SignalR Server is running on " + signalRServerUrl);
                }
                if (config == "5" || config == "6")
                {
                    if (config == "5")
                        notifierKey = "RedisNotifier";
                    if (config == "6")
                        cacheKey = "RedisCache";

                    Console.WriteLine("Starting Redis Server...");
                    startRedis(redisConf);
                }

                Console.Write("How many clients do you wish to spawn?");
                if (!int.TryParse(Console.ReadLine(), out clientCount))
                    clientCount = 2;


                Console.WriteLine("Starting {0} clients...", clientCount);
                for (var i = 0; i < clientCount; i++)
                {
                    startClient(cacheKey + " " + monitorKey + " " + notifierKey + " " + signalRServerUrl, "SampleClient" + (i + 1));
                }

                Console.WriteLine("Press any key to quit.");
                Console.ReadLine();
            }
        }

        private static void startClient(string url, string name)
        {
            var process = new ProcessStartInfo() { FileName = Environment.CurrentDirectory.PathCombine(@"..\..\CodeEndeavors.Distributed.Cache.SampleClient\bin\CodeEndeavors.Distributed.Cache.SampleClient.exe", @"\"), Arguments = url + " " + name };
            System.Diagnostics.Process.Start(process);
        }

        private static void startRedis(string redisConf)
        {
            var workingDirectory = Environment.CurrentDirectory.PathCombine(@"..\..\packages\Redis-64.3.0.501\tools\", @"\");
            var info = new ProcessStartInfo() { FileName = workingDirectory.PathCombine("redis-server.exe", @"\"), Arguments = redisConf };
            info.WorkingDirectory = workingDirectory;
            //info.RedirectStandardOutput = true;
            //info.UseShellExecute = false;
            var process = System.Diagnostics.Process.Start(info);
            
            //while (!process.StandardOutput.EndOfStream)
            //{
            //    Console.WriteLine(process.StandardOutput.ReadLine());
            //}
        }

    }
}
