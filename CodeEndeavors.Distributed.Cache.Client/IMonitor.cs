using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client
{
    public interface IMonitor:  IDisposable
    {
        bool Initialize(string cacheName, string key, dynamic options);
        bool Initialize(string cacheName, string key, string itemKey, dynamic options);

        string CacheName { get; set; }
        string CacheKey { get; set; }
        string CacheItemKey { get; set; }

        event Action<string, string> OnExpire;
        event Action<string, string, string> OnExpireItem;

    }
}
