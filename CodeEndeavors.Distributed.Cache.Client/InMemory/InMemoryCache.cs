using System;
using CodeEndeavors.Extensions;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace CodeEndeavors.Distributed.Cache.Client.InMemory
{
    public class InMemoryCache : ICache 
    {
        private Dictionary<string, object> _connection = new Dictionary<string,object>();
        private string _cacheName;

        public string ClientId {get;set;}
        public string Name { get { return "InMemoryCache"; } }

        public string NotifierName { get; set; }

        public bool Initialize(string cacheName, string clientId, string notifierName, string connection)
        {
            _cacheName = cacheName;
            ClientId = clientId;
            NotifierName = notifierName;
            _connection = connection.ToObject<Dictionary<string, object>>();

            log(Logging.LoggingLevel.Minimal, "Initialized");
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
            SetExp<T>(key, null, value);
        }
        public void SetExp<T>(string key, TimeSpan? absoluteExpiration, T value)
        {
            MemoryCache.Default.Set(key, value, getCacheItemPolicy(absoluteExpiration));
        }

        private CacheItemPolicy getCacheItemPolicy(TimeSpan? absoluteExpiration)
        {
            var policy = new CacheItemPolicy();
            if (_connection.ContainsKey("slidingExpiration"))
                policy.SlidingExpiration = _connection.GetSetting("slidingExpiration", "'00:00:00'").ToObject<TimeSpan>();
            if (!absoluteExpiration.HasValue && _connection.ContainsKey("absoluteExpiration"))
                absoluteExpiration = _connection.GetSetting("absoluteExpiration", "'00:00:00'").ToObject<TimeSpan>();
            if (absoluteExpiration.HasValue)
                policy.AbsoluteExpiration = DateTimeOffset.Now.Add(absoluteExpiration.Value);
            return policy;
        }

        public void Set<T>(string key, string itemKey, T value)
        {
            SetExp<T>(key, itemKey, null, value);
        }
        public void SetExp<T>(string key, string itemKey, TimeSpan? absoluteExpiration, T value)
        {
            var dict = getItemDictionary(key, true, absoluteExpiration);
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
            return getItemDictionary(key, false, null);
        }
        private Dictionary<string, object> getItemDictionary(string key, bool insertIfMissing)
        {
            return getItemDictionary(key, insertIfMissing, null);
        }
        private Dictionary<string, object> getItemDictionary(string key, bool insertIfMissing, TimeSpan? absoluteExpiration)
        {
            var dict = MemoryCache.Default.Get(key) as Dictionary<string, object>;
            if (dict == null)
            {
                dict = new Dictionary<string, object>();
                if (insertIfMissing)
                    SetExp(key, absoluteExpiration, dict);
            }
            return dict;
        }

        public void Dispose()
        {
            MemoryCache.Default.Dispose();
        }

        #region Logging
        protected void log(Logging.LoggingLevel level, string msg)
        {
            log(level, msg, "");
        }
        protected void log(Logging.LoggingLevel level, string msg, params object[] args)
        {
            Logging.Log(level, string.Format("[{0}:{1}:{2}] - {3}", Name, ClientId, _cacheName, string.Format(msg, args)));
        }
        #endregion
    }
}
