using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client
{
    public interface ICache : IDisposable
    {
        string ClientId {get;set;}
        string NotifierName { get; set; }

        bool Initialize(string cacheName, string clientId, string notifierName, string connection);

        bool Exists(string key);
        bool Exists(string key, string itemKey);

        T Get<T>(string key, T defaultValue);
        T Get<T>(string key, string itemKey, T defaultValue);

        bool GetExists<T>(string key, out T entry);
        bool GetExists<T>(string key, string itemKey, out T entry);

        void Set<T>(string key, TimeSpan? absoluteExpiration, T value);
        void Set<T>(string key, string itemKey, TimeSpan? absoluteExpiration, T value);

        void ListPush<T>(string key, TimeSpan? absoluteExpiration, T[] values);

        bool Remove(string key);
        bool Remove(string key, string itemKey);

        bool Clear();

        //void Expire(string key);

    }

    public interface IStaleCache : ICache
    {
        //bool Exists(string key, out bool isStale);
        bool GetExists<T>(string key, out bool isStale, out T entry);
        //bool GetExists<T>(string key, string itemKey, out bool isStale, out T entry);
        //T Get<T>(string key, out bool isStale, T defaultValue);
    }

}
