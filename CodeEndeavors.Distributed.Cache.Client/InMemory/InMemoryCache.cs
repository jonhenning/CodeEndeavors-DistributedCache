using System;
using CodeEndeavors.Extensions;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace CodeEndeavors.Distributed.Cache.Client.InMemory
{
    public class InMemoryCache : ICache 
    {
        private string _connection;
        private string _cacheName;
        private string _clientId;

        public string ClientId {get;set;}
        public string NotifierName { get; set; }

        public bool Initialize(string cacheName, string clientId, string notifierName, string connection)
        {
            _connection = connection;
            _cacheName = cacheName;
            _clientId = clientId;
            NotifierName = notifierName;
            var connectionDict = connection.ToObject<Dictionary<string, object>>();

            return true;
        }

        public bool Exists(string key)
        {
            return MemoryCache.Default.Contains(key);
        }
        public bool Exists(string key, string itemKey)
        {
            return getItemDictionary(key).ContainsKey(itemKey);
        }

        public bool GetExists<T>(string key, out T entry)
        {
            entry = default(T);
            var item = MemoryCache.Default.Get(key);
            if (item == null)
                return false;
            entry = item.ToType<T>();
            return true;
        }

        public bool GetExists<T>(string key, string itemKey, out T entry)
        {
            entry = default(T);
            var dict = getItemDictionary(key);
            entry = dict.GetSetting<T>(itemKey, entry);
            return dict.ContainsKey(itemKey);
        }

        public T Get<T>(string key, T defaultValue)
        {
            var ret = MemoryCache.Default.Get(key);
            if (ret == null)
                return defaultValue;
            return ret.ToType<T>();
        }

        public T Get<T>(string key, string itemKey, T defaultValue)
        {
            return getItemDictionary(key).GetSetting<T>(itemKey, defaultValue);
        }

        public void Set<T>(string key, T value)
        {
            var policy = new CacheItemPolicy(); 
            MemoryCache.Default.Set(key, value, policy);

        }
        public void Set<T>(string key, string itemKey, T value)
        {
            var dict = getItemDictionary(key, true);
            dict[itemKey] = value;
        }

        public bool Remove(string key)
        {
            var ret = remove(key);
            return ret;
        }

        public bool Remove(string key, string itemKey)
        {
            var dict = getItemDictionary(key);
            return dict.Remove(itemKey);
        }

        private bool remove(string key)
        {
            return MemoryCache.Default.Remove(key) != null;
        }

        private Dictionary<string, object> getItemDictionary(string key)
        {
            return getItemDictionary(key, false);
        }
        private Dictionary<string, object> getItemDictionary(string key, bool insertIfMissing)
        {
            var dict = MemoryCache.Default.Get(key) as Dictionary<string, object>;
            if (dict == null)
            {
                dict = new Dictionary<string, object>();
                if (insertIfMissing)
                    Set(key, dict);
            }
            return dict;
        }

        public void Dispose()
        {
            MemoryCache.Default.Dispose();
        }
    }
}
