using CodeEndeavors.Extensions;
using CodeEndeavors.Distributed.Cache.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

namespace CodeEndeavors.Distributed.Cache.SampleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string notifierName = "TestRedisNotifier";// "TestSignalRNotifier";
                string cacheName = "TestSignalR";
                string url = "";
                string clientId = "SampleClient";
                var redisServer = "127.0.0.1";
                if (args.Length > 0)
                    url = args[0];
                if (args.Length > 1)
                    clientId = args[1];

                Console.WriteLine("{0} Started...", clientId);

                Service.OnNotifierMessage += (cid, message) =>
                {
                    Console.WriteLine("MSG: clientId:" + cid + "\r\n    " + message);
                };

                Service.OnCacheExpire += (cid, key) =>
                {
                    Console.WriteLine("EXP: " + cid + " - " + key);
                };
                Service.OnCacheItemExpire += (cid, key, itemKey) =>
                {
                    Console.WriteLine("EXP: " + cid + " - " + key + " - " + itemKey);
                };

                //Service.OnError += (Exception ex) =>
                //{
                //    Console.WriteLine(ex.Message);
                //};

                Console.ReadLine();
                var configFileName = AppDomain.CurrentDomain.BaseDirectory.PathCombine("CodeEndeavors.Distributed.Cache.SampleClient.exe.config", "\\");
                
                var notifierConnection = "";
                var cacheConnection = "";
                //var monitorOptions = "";
                dynamic monitorOptions;
                notifierConnection = string.Format("{{'notifierType': 'CodeEndeavors.Distributed.Cache.Client.SignalR.SignalRNotifier', 'clientId': '{0}', 'url': '{1}'}}", clientId, url);
                //notifierConnection = string.Format("{{'notifierType': 'CodeEndeavors.Distributed.Cache.Client.Redis.RedisNotifier', 'clientId': '{0}', 'server': '{1}'}}", clientId, redisServer);
                cacheConnection = string.Format("{{'cacheType': 'CodeEndeavors.Distributed.Cache.Client.InMemory.InMemoryCache', 'notifierName': '{0}', 'clientId': '{1}' }}", notifierName, clientId);
                //cacheConnection = string.Format("{{'cacheType': 'CodeEndeavors.Distributed.Cache.Client.Redis', 'notifierName': '{0}', 'clientId': '{1}', 'server': '{2}' }}", notifierName, clientId, redisServer);

                //monitorOptions = (new { monitorType = "CodeEndeavors.Distributed.Cache.Client.File.FileMonitor", fileName = configFileName, uniqueProperty = "fileName" }).ToJson();
                monitorOptions = new { monitorType = "CodeEndeavors.Distributed.Cache.Client.File.FileMonitor", fileName = configFileName, uniqueProperty = "fileName" };

                Console.WriteLine("Notifier Connection: " + notifierConnection);
                Console.WriteLine("Cache Connection: " + cacheConnection);
                Console.WriteLine("Monitor Options: " + monitorOptions);

                if (!string.IsNullOrEmpty(notifierConnection))
                    CodeEndeavors.Distributed.Cache.Client.Service.RegisterNotifier(notifierName, notifierConnection);

                CodeEndeavors.Distributed.Cache.Client.Service.RegisterCache(cacheName, cacheConnection);

                //var cache = Service.getCache(cacheName);

                Console.WriteLine("Connected!");

                Console.WriteLine("Type 'exit' to stop");
                var command = "";

                while (!command.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        Console.Write(">");
                        command = Console.ReadLine();
                        if (command.StartsWith("set "))
                        {
                            var a = command.Split(' ');
                            if (a.Length > 2)
                            {
                                var key = a[1];
                                var value = a[2];
                                Console.WriteLine("Calling SetCacheEntry({0}, {1}, {2})", cacheName, key, value);
                                Service.SetCacheEntry(cacheName, key, value, monitorOptions);
                            }
                            else
                                Console.WriteLine("invalid usage");
                        }
                        else if (command.StartsWith("get "))
                        {
                            var key = command.Split(' ')[1];
                            Console.WriteLine("Calling GetCacheEntry({0}, {1})", cacheName, key);
                            Console.WriteLine(Service.GetCacheEntry<string>(cacheName, key, ""));
                        }
                        else if (command.StartsWith("remove "))
                        {
                            var key = command.Split(' ')[1];
                            Console.WriteLine("Calling RemoveCacheEntry({0}, {1})", cacheName, key);
                            Console.WriteLine(Service.RemoveCacheEntry(cacheName, key));
                        }
                        else if (command.StartsWith("getlookup "))
                        {
                            var key = command.Split(' ')[1];
                            Console.WriteLine("Calling GetCacheEntry({0}, {1})", cacheName, key);
                            Console.WriteLine(Service.GetCacheEntry(cacheName, key, () =>
                            {
                                simulateLongRunningTask();
                                return new List<string>() { "A", "B", "C", DateTime.Now.ToString() };
                            }).ToJson(true));
                        }
                        else if (command.StartsWith("getsparselookup "))
                        {
                            var key = command.Split(' ')[1];
                            var itemKey = command.Split(' ')[2];
                            Console.WriteLine("Calling GetCacheEntry({0}, {1}, {2})", cacheName, key, itemKey);
                            Console.WriteLine(Service.GetCacheEntry(cacheName, key, itemKey, () =>
                            {
                                simulateLongRunningTask();
                                return DateTime.Now.ToString();
                            }).ToJson(true));
                        }
                        else if (command.StartsWith("getmultilookup "))
                        {
                            var key = command.Split(' ')[1];
                            var itemKeys = command.Split(' ')[2].Split(',').ToList();
                            Console.WriteLine("Calling GetCacheEntry({0}, {1}, {2})", cacheName, key, itemKeys.ToJson());
                            Console.WriteLine(Service.GetCacheEntry(cacheName, key, itemKeys, (keys) =>
                            {
                                simulateLongRunningTask();
                                return keys.Select(k => new {key = k, value = DateTime.Now.ToString()}).ToDictionary(x => x.key, x => x.value);
                            }).ToJson(true));
                        }
                        else if (command.StartsWith("expire "))
                        {
                            var a = command.Split(' ');
                            var key = a[1];
                            if (a.Length > 2)
                            {
                                var itemKey = a[2];
                                Console.WriteLine("Calling ExpireCacheItemEntry({0}, {1})", key, itemKey);
                                Service.ExpireCacheItemEntry(cacheName, key, itemKey);
                            }
                            else
                            {
                                Console.WriteLine("Calling ExpireCacheEntry({0})", key);
                                Service.ExpireCacheEntry(cacheName, key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();

        }
        private static void simulateLongRunningTask()
        {
            Console.Write("Simulating long running task...");
            for (var i = 0; i < 5; i++)
            {
                Console.Write(".");
                System.Threading.Thread.Sleep(500);
            }
            Console.WriteLine(".");
        }
    }
}
