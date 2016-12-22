using CodeEndeavors.Distributed.Cache.Client.Extensions;
using CodeEndeavors.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System.Dynamic;
using System.IO;
using System.Reflection;
using Logging = CodeEndeavors.Distributed.Cache.Client.Logging;

namespace CodeEndeavors.Distributed.Cache.Client
{
    /// <summary>
    /// This is the primary class used to interact with your cache
    /// </summary>
    public class Service
    {
        private static readonly object cacheLock = new object();
        private static ConcurrentDictionary<string, ICache> _caches = new ConcurrentDictionary<string, ICache>();
        private static ConcurrentDictionary<string, INotifier> _notifiers = new ConcurrentDictionary<string, INotifier>();
        private static ConcurrentDictionary<string, IMonitor> _monitors = new ConcurrentDictionary<string, IMonitor>();

        /// <summary>
        /// Global event handling all Notifier's events
        /// </summary>
        public static event Action<string, string> OnNotifierMessage;
        /// <summary>
        /// Global event handling all Expire events
        /// </summary>
        public static event Action<string, string> OnCacheExpire;
        /// <summary>
        /// Global event handling all ExpireItem events
        /// </summary>
        public static event Action<string, string, string> OnCacheItemExpire;
        //public static event Action<Exception> OnError;

        /// <summary>
        /// Method used to first inspect cache for results and if not found invoke the lookupFunc to populate the cache with results for subsequent calls.
        /// </summary>
        /// <typeparam name="T">Type to be stored in cache</typeparam>
        /// <param name="cacheName">Name of cache to get/store results in</param>
        /// <param name="cacheKey">Key in cache to get/store results in</param>
        /// <param name="lookupFunc">delegate to invoke when entry not found in cache</param>
        /// <returns>results (either cached or looked up) from lookupFunc</returns>
        public static T GetCacheEntry<T>(string cacheName, string cacheKey, Func<T> lookupFunc)
        {
            var e = GetCacheEntry<T>(cacheName, cacheKey, lookupFunc, null);
            return (T)e;
        }
        public static T GetCacheEntry<T>(string cacheName, string cacheKey, Func<T> lookupFunc, dynamic monitorOptions)
        {
            var e = GetCacheEntry<T>(cacheName, (TimeSpan?)null, cacheKey, lookupFunc, monitorOptions);
            return (T)e;
        }

        public static T GetCacheEntry<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, Func<T> lookupFunc)
        {
            var e = GetCacheEntry<T>(cacheName, absoluteExpiration, cacheKey, lookupFunc, null);
            return (T)e;
        }

        public static T GetCacheEntry<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, Func<T> lookupFunc, dynamic monitorOptions)
        {
            var cache = getCache(cacheName);

            if (cache == null)  //not using cache
                return lookupFunc();

            T item = default(T);

            if (!cache.GetExists<T>(cacheKey, out item))
            {
                lock (cache)
                {
                    if (!cache.Exists(cacheKey))
                    {
                        using (new Client.OperationTimer("GetCacheEntry (lookup): {0}:{1}", cacheName, cacheKey))
                            item = lookupFunc();
                        //cache.Set(cacheKey, item);
                        //SetCacheEntry(cacheName, cacheKey, item, monitorOptions);
                        SetCacheEntry<T>(cacheName, absoluteExpiration, cacheKey, item);
                        Logging.Log(Logging.LoggingLevel.Minimal, "Retrieved cache entry {0}:{1}", cacheName, cacheKey);
                    }
                    else
                        using (new Client.OperationTimer("GetCacheEntry (in-cache): {0}:{1}", cacheName, cacheKey))
                            item = cache.Get(cacheKey, default(T));
                }
            }

            //always attempt to register monitor if we have one
            //for scenarios where we may be sharing a cache with another, they may have put the item in cache
            //we still want to register ourselves for the changed events to properly expire
            //even though they may be responsible for the expire, they may have been shut down
            if (monitorOptions != null)
                RegisterMonitor(cacheName, cacheKey, monitorOptions);

            return item;
        }

        /// <summary>
        /// Method used to first inspect cache for results and if not found invoke the lookupFunc to populate the cache with results for subsequent calls.
        /// The actual entry in the cache will be a Dictionary<string, T> where the Dictionary is referred to by the cacheKey and the string (key) is referred to by itemKey
        /// </summary>
        /// <typeparam name="T">Type to be stored in cache</typeparam>
        /// <param name="cacheName">Name of cache to get/store results in</param>
        /// <param name="cacheKey">Key in cache where Dictionary is stored</param>
        /// <param name="itemKey">key in dictionary where results of lookupFunc are stored</param>
        /// <param name="lookupFunc">delegate to invoke when entry not found in cache</param>
        /// <returns>results (either cached or looked up) from lookupFunc</returns>
        public static T GetCacheEntry<T>(string cacheName, string cacheKey, string itemKey, Func<T> lookupFunc)
        {
            return GetCacheEntry<T>(cacheName, null, cacheKey, itemKey, lookupFunc);
        }

        public static T GetCacheEntry<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, string itemKey, Func<T> lookupFunc)
        {
            var cache = getCache(cacheName);

            if (cache == null)  //not using cache
                return lookupFunc();

            T item = default(T);

            if (!cache.GetExists<T>(cacheKey, itemKey, out item))
            {
                lock (cache)
                {
                    if (!cache.Exists(cacheKey, itemKey))
                    {
                        using (new Client.OperationTimer("GetCacheEntry (lookup): {0}:{1}:{2}", cacheName, cacheKey, itemKey))
                            item = lookupFunc();
                        cache.Set(cacheKey, itemKey, absoluteExpiration, item);
                        Logging.Log(Logging.LoggingLevel.Minimal, "Retrieved cache entry {0}:{1}:{2}", cacheName, cacheKey, itemKey);
                    }
                    else
                        using (new Client.OperationTimer("GetCacheEntry (in-cache): {0}:{1}:{2}", cacheName, cacheKey, itemKey))
                            item = cache.Get(cacheKey, itemKey, default(T));
                }
            }
            return item;
        }
        /// <summary>
        /// Method used to first inspect cache for results and if not found invoke the lookupFunc to populate the cache with results for subsequent calls.
        /// The actual entry in the cache will be a Dictionary<string, T> where the Dictionary is referred to by the cacheKey and the string (key) is referred to by itemKey
        /// Passing in multiple keys will first look in the Dictionary for each one, only the ones not found will be passed to the lookupFunc call
        /// </summary>
        /// <typeparam name="T">Type to be stored in cache</typeparam>
        /// <param name="cacheName">Name of cache to get/store results in</param>
        /// <param name="cacheKey">Key in cache where Dictionary is stored</param>
        /// <param name="itemKeys">entries in dictionary where results of lookupFunc are stored</param>
        /// <param name="lookupFunc">delegate to invoke when entry not found in cache</param>
        /// <returns>results (either cached or looked up) from lookupFunc</returns>
        public static Dictionary<string, T> GetCacheEntry<T>(string cacheName, string cacheKey, List<string> itemKeys, Func<List<string>, Dictionary<string, T>> lookupFunc)
        {
            return GetCacheEntry<T>(cacheName, null, cacheKey, itemKeys, lookupFunc);
        }

        public static Dictionary<string, T> GetCacheEntry<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, List<string> itemKeys, Func<List<string>, Dictionary<string, T>> lookupFunc)
        {
            var ret = new Dictionary<string, T>();

            var cache = getCache(cacheName);
            var keysToLookup = new List<string>();

            foreach (var itemKey in itemKeys)
            {
                T item = default(T);

                if (cache == null || !cache.GetExists<T>(cacheKey, itemKey, out item))
                    keysToLookup.Add(itemKey);
                else
                    ret[itemKey] = item;
            }
            if (keysToLookup.Count > 0)
            {
                var newValues = lookupFunc.Invoke(keysToLookup);
                if (cache == null)
                    return newValues;

                foreach (var itemKey in newValues.Keys)
                    cache.Set(cacheKey, itemKey, absoluteExpiration, newValues[itemKey]);
                ret.Merge(newValues, false);
                Logging.Log(Logging.LoggingLevel.Minimal, "Retrieved cache entries {0} {1}", cacheName, keysToLookup.ToJson());
            }
            return ret;
        }

        /// <summary>
        /// Allows cache entry to be stored
        /// </summary>
        /// <typeparam name="T">Data type of value stored</typeparam>
        /// <param name="cacheName">Name of cache to store results in</param>
        /// <param name="cacheKey">Key in cache where entry is stored</param>
        /// <param name="value"></param>
        public static void SetCacheEntry<T>(string cacheName, string cacheKey, T value)
        {
            SetCacheEntry<T>(cacheName, cacheKey, value, null);
        }
        public static void SetCacheEntry<T>(string cacheName, string cacheKey, T value, dynamic monitorOptions)
        {
            SetCacheEntry<T>(cacheName, null, cacheKey, value, monitorOptions);
        }

        public static void SetCacheEntry<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, T value)
        {
            SetCacheEntry<T>(cacheName, absoluteExpiration, cacheKey, value, null);
        }

        public static void SetCacheEntry<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, T value, dynamic monitorOptions)
        {
            using (new Client.OperationTimer("SetCacheEntry: {0}:{1}", cacheName, cacheKey))
            {
                var cache = getCache(cacheName);
                if (cache != null)
                {
                    cache.Set<T>(cacheKey, absoluteExpiration, value);
                    //if (!string.IsNullOrEmpty(monitorOptions))
                    if (monitorOptions != null)
                        RegisterMonitor(cacheName, cacheKey, monitorOptions);
                }
            }
        }

        public static void SetCacheEntry<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, string itemKey, T value, dynamic monitorOptions)
        {
            using (new Client.OperationTimer("SetCacheEntry: {0}:{1}:{2}", cacheName, cacheKey, itemKey))
            {
                var cache = getCache(cacheName);
                if (cache != null)
                {
                    cache.Set<T>(cacheKey, itemKey, absoluteExpiration, value);
                    //if (!string.IsNullOrEmpty(monitorOptions))
                    if (monitorOptions != null)
                        RegisterMonitor(cacheName, cacheKey, monitorOptions);
                }
            }
        }

        /// <summary>
        /// Creates list if one does not already exist and adds item to it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheName">Name of cache to store results in</param>
        /// <param name="cacheKey">Key in cache where entry is stored</param>
        /// <param name="value"></param>
        /// <param name="monitorOptions"></param>
        public static void ListPush<T>(string cacheName, string cacheKey, T value, dynamic monitorOptions = null)
        {
            ListPush<T>(cacheName, cacheKey, new T[] { value }, monitorOptions);
        }
        public static void ListPush<T>(string cacheName, string cacheKey, T[] values, dynamic monitorOptions = null)
        {
            ListPush<T>(cacheName, null, cacheKey, values, monitorOptions);
        }

        public static void ListPush<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, T value, dynamic monitorOptions = null)
        {
            ListPush<T>(cacheName, absoluteExpiration, cacheKey, new T[] { value }, monitorOptions);
        }
        public static void ListPush<T>(string cacheName, TimeSpan? absoluteExpiration, string cacheKey, T[] values, dynamic monitorOptions = null)
        {
            using (new Client.OperationTimer("ListPush: {0}:{1}", cacheName, cacheKey))
            {
                var cache = getCache(cacheName);
                if (cache != null)
                {
                    cache.ListPush<T>(cacheKey, absoluteExpiration, values);
                    if (monitorOptions != null)
                        RegisterMonitor(cacheName, cacheKey, monitorOptions);
                }
            }
        }

        /// <summary>
        /// Retrieves value from cache
        /// </summary>
        /// <typeparam name="T">Data type of value retrieved</typeparam>
        /// <param name="cacheName">Name of cache to get results from</param>
        /// <param name="cacheKey">Key in cache where entry is stored</param>
        /// <param name="defaultValue">Value to return if item not found in cache</param>
        /// <returns></returns>
        public static T GetCacheEntry<T>(string cacheName, string cacheKey, T defaultValue)
        {
            using (new Client.OperationTimer("GetCacheEntry: {0}:{1}", cacheName, cacheKey))
            {
                var cache = getCache(cacheName);
                if (cache == null)
                    return defaultValue;
                return cache.Get<T>(cacheKey, defaultValue);
            }
        }

        /// <summary>
        /// Removes entry from cache
        /// </summary>
        /// <param name="cacheName">Name of cache to get results from</param>
        /// <param name="cacheKey">Key in cache where entry is stored</param>
        /// <returns>true if item was removed, false if item not found</returns>
        public static bool RemoveCacheEntry(string cacheName, string cacheKey)
        {
            bool success;
            using (new Client.OperationTimer("RemoveCacheEntry: {0}:{1}", cacheName, cacheKey))
            {
                var cache = getCache(cacheName);
                if (cache == null)
                    return false;
                success = cache.Remove(cacheKey);
            }
            Logging.Log(Logging.LoggingLevel.Minimal, "Removed cache entry {0}:{1} ({2})", cacheName, cacheKey, success);
            return success;
        }
        /// <summary>
        /// Removes entry from cache Dictionary
        /// </summary>
        /// <param name="cacheName">Name of cache to get results from</param>
        /// <param name="cacheKey">Key in cache where Dictionary is stored</param>
        /// <param name="itemKey">Key of dictionary entry to remove</param>
        /// <returns>true if item was removed, false if item not found</returns>
        public static bool RemoveCacheEntry(string cacheName, string cacheKey, string itemKey)
        {
            bool success;
            using (new Client.OperationTimer("RemoveCacheEntry: {0}:{1}:{2}", cacheName, cacheKey, itemKey))
            {
                var cache = getCache(cacheName);
                if (cache == null)
                    return false;
                success = cache.Remove(cacheKey, itemKey);
            }
            Logging.Log(Logging.LoggingLevel.Minimal, "Removed cache entry {0}:{1}:{2} ({3})", cacheName, cacheKey, itemKey, success);
            return success;
        }

        /// <summary>
        /// Expires entry from cache
        /// </summary>
        /// <remarks>
        /// If notifier attached to cache, a message will be broadcast to expire all caches matching the cacheName and cacheKey
        /// </remarks>
        /// <param name="cacheName">Name of cache to get results from</param>
        /// <param name="cacheKey">Key in cache where entry is stored</param>
        public static void ExpireCacheEntry(string cacheName, string cacheKey)
        {
            using (new Client.OperationTimer("ExpireCacheEntry: {0}:{1}", cacheName, cacheKey))
                expireCacheEntry(cacheName, cacheKey, true);
        }

        /// <summary>
        /// Expires entry from cache Dictionary
        /// </summary>
        /// <remarks>
        /// If notifier attached to cache, a message will be broadcast to expire all caches matching the cacheName, cacheKey, and itemKey
        /// </remarks>
        /// <param name="cacheName">Name of cache to get results from</param>
        /// <param name="cacheKey">Key in cache where Dictionary is stored</param>
        /// <param name="itemKey">Key of dictionary entry to expire</param>
        public static void ExpireCacheItemEntry(string cacheName, string cacheKey, string itemKey)
        {
            using (new Client.OperationTimer("ExpireCacheEntry: {0}:{1}:{2}", cacheName, cacheKey, itemKey))
                expireCacheItemEntry(cacheName, cacheKey, itemKey, true);
        }

        /// <summary>
        /// Maintains a list of cachekeys dependent upon a type:typeKey entry
        /// </summary>
        /// <param name="cacheName">Name of cache to get results from</param>
        /// <param name="type">Type of dependency (i.e. Table)</param>
        /// <param name="typeKey">Key of type dependency (i.e. TableName)</param>
        /// <param name="cacheKey">Cache key</param>
        public static void AddCacheDependency(string cacheName, string type, string typeKey, string cacheKey)
        {
            AddCacheDependency(cacheName, null, type, typeKey, cacheKey);
        }

        public static void AddCacheDependency(string cacheName, TimeSpan? absoluteExpiration, string type, string typeKey, string cacheKey)
        {
            ListPush(cacheName, string.Format("{0}:{1}", type, typeKey), cacheKey);
        }

        public static void ExpireCacheDependencies(string cacheName, string type, string typeKey)
        {
            var key = string.Format("{0}:{1}", type, typeKey);
            var entries = GetCacheEntry(cacheName, key, new List<object>());
            foreach (var cacheKey in entries)
                ExpireCacheEntry(cacheName, cacheKey.ToString());
            RemoveCacheEntry(cacheName, key);
        }

        /// <summary>
        /// Registers a cache to be used
        /// </summary>
        /// <example>
        /// RegisterCache("MyCache", "{'cacheType': 'CodeEndeavors.Distributed.Cache.Client.InMemory.InMemoryCache', 'notifierName': 'MyNotifier', 'clientId': 'MyClient1' }");
        /// </example>
        /// <param name="cacheName">Name of cache to register</param>
        /// <param name="connection">JSON connection string specifying cacheType and other options for cache provider</param>
        /// <returns>Cache reference</returns>
        public static ICache RegisterCache(string cacheName, string connection)
        {
            if (!_caches.ContainsKey(cacheName))
            {
                lock (cacheLock)
                {
                    if (!_caches.ContainsKey(cacheName))
                    {
                        using (new Client.OperationTimer("RegisterCache: {0}:{1}", cacheName, connection))
                        {
                            var connectionDict = connection.ToObject<Dictionary<string, object>>();
                            var typeName = connectionDict.GetSetting("cacheType", "CodeEndeavors.Distributed.Cache.Client.InMemory.InMemoryCache");
                            var clientId = connectionDict.GetSetting("clientId", Guid.NewGuid().ToString());

                            var cache = typeName.GetInstance<ICache>();
                            if (cache.Initialize(cacheName, clientId, connectionDict.GetSetting("notifierName", ""), connection))
                            {
                                _caches[cacheName] = cache;
                                return cache;
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Registers notifier to be used
        /// </summary>
        /// <example>
        /// RegisterNotifier("{'notifierType': 'CodeEndeavors.Distributed.Cache.Client.Redis.RedisNotifier', 'clientId': 'MyClient1', 'server': '127.0.0.1'}}");
        /// </example>
        /// <param name="name">Name of notifier</param>
        /// <param name="connection">JSON connection string specifying the notifierType and other options for the notifier provider</param>
        /// <returns></returns>
        public static INotifier RegisterNotifier(string name, string connection)
        {
            if (!_notifiers.ContainsKey(name))
            {
                lock (cacheLock)
                {
                    if (!_notifiers.ContainsKey(name))
                    {
                        using (new Client.OperationTimer("RegisterNotifier: {0}:{1}", name, connection))
                        {
                            var connectionDict = connection.ToObject<Dictionary<string, object>>();
                            var typeName = connectionDict.GetSetting("notifierType", "CodeEndeavors.Distributed.Cache.Client.SignalRNotifier");
                            var clientId = connectionDict.GetSetting("clientId", Guid.NewGuid().ToString());

                            var notifier = typeName.GetInstance<INotifier>();
                            notifier.OnMessage += onNotifierMessage;
                            if (notifier.Initialize(clientId, connection))
                            {
                                _notifiers[name] = notifier;
                                notifier.OnExpire += onCacheExpire;
                                notifier.OnExpireItem += onCacheItemExpire;

                                return notifier;
                            }
                        }
                    }
                }
            }
            return null;
        }
      
        //todo: use json string dictionary or dynamic type?
        public static void RegisterMonitor(string cacheName, string cacheKey, dynamic options)
        {
            RegisterMonitor(cacheName, cacheKey, null, options);
        }

        public static void RegisterMonitor(string cacheName, string cacheKey, string cacheItemKey, dynamic options)
        {
            using (new Client.OperationTimer("RegisterMonitor: {0}:{1}:{2}", cacheName, cacheKey, cacheItemKey))
            {
                //var optionDict = options.ToObject<Dictionary<string, object>>();
                //var optionDict = new Dictionary<string, object>((IDictionary<string, object>)options);
                var optionDict = ((object)options).ToDictionary(); //options.ToJson().ToObject<Dictionary<string, object>>();
                //allow options to contain a propertyName that is identified as unique or a uniqueId
                var uniqueId = optionDict.GetSetting(optionDict.GetSetting("uniqueProperty", "uniqueId"), "");

                if (string.IsNullOrEmpty(uniqueId))
                    throw new Exception("Monitor must contain uniqueId in options");

                var key = getMonitorKey(cacheName, cacheKey, cacheItemKey, uniqueId);
                if (!_monitors.ContainsKey(key))
                {
                    var typeName = optionDict.GetSetting("monitorType", "CodeEndeavors.Distributed.Cache.Client.File.FileMonitor");
                    var monitor = typeName.GetInstance<IMonitor>();
                    if (monitor.Initialize(cacheName, cacheKey, cacheItemKey, options))
                    {
                        monitor.OnExpire += onCacheExpire;
                        monitor.OnExpireItem += onCacheItemExpire;
                        _monitors[key] = monitor;

                        Logging.Log(Logging.LoggingLevel.Minimal, "Registered Monitor {0}:{1}:{2} ({3})", cacheName, cacheKey, cacheItemKey, uniqueId);
                    }
                }
            }
        }

        private static string getMonitorKey(string cacheName, string cacheKey, string cacheItemKey, string uniqueId)
        {
            return string.Format("{0}~{1}~{2}~{3}", cacheName, cacheKey, cacheItemKey, uniqueId);
        }

        /// <summary>
        /// Disposes of all resources
        /// </summary>
        public static void Dispose()
        {
            lock (cacheLock)
            {
                if (_caches != null)
                    _caches.Values.ToList().ForEach(c => c.Dispose());
                _caches = new ConcurrentDictionary<string, ICache>();
                if (_notifiers != null)
                    _notifiers.Values.ToList().ForEach(n => n.Dispose());
                _notifiers = new ConcurrentDictionary<string, INotifier>();
                if (_monitors != null)
                    _monitors.Values.ToList().ForEach(m => m.Dispose());
                _monitors = new ConcurrentDictionary<string, IMonitor>();

                Logging.Log(Logging.LoggingLevel.Minimal, "Disposed of all resources");
            }
        }

        private static ICache getCache(string name)
        {
            ICache cache = null;
            if (!string.IsNullOrEmpty(name))
                _caches.TryGetValue(name, out cache);

            if (cache != null)
            {
                Logging.Log(Logging.LoggingLevel.Verbose, "Retrieved cache {0}", name);
                return cache;
            }
            if (!string.IsNullOrEmpty(name))
                throw new Exception("Cache not registered: " + name);
            else
                Logging.Log(Logging.LoggingLevel.Verbose, "Cache Not Used");
            return cache;
        }

        private static INotifier getNotifier(string name)
        {
            INotifier notifier = null;
            _notifiers.TryGetValue(name, out notifier);
            if (notifier != null)
            {
                Logging.Log(Logging.LoggingLevel.Verbose, "Retrieved notifier {0}", name);
                return notifier;
            }
            throw new Exception("Notifier not registered: " + name);
        }

        private static void expireCacheEntry(string cacheName, string cacheKey, bool broadcast)
        {
            RemoveCacheEntry(cacheName, cacheKey);

            if (Service.OnCacheExpire != null)
                Service.OnCacheExpire(cacheName, cacheKey);

            var cache = getCache(cacheName);
            if (cache != null && broadcast && !string.IsNullOrEmpty(cache.NotifierName))
            {
                Logging.Log(Logging.LoggingLevel.Minimal, "Broadcasting expire {0}:{1}", cacheName, cacheKey);
                getNotifier(cache.NotifierName).BroadcastExpireCache(cacheName, cacheKey);
            }
        }

        private static void expireCacheItemEntry(string cacheName, string cacheKey, string itemKey, bool broadcast)
        {
            RemoveCacheEntry(cacheName, cacheKey, itemKey);
            if (Service.OnCacheItemExpire != null)
                Service.OnCacheItemExpire(cacheName, cacheKey, itemKey);
            var cache = getCache(cacheName);
            if (cache != null && broadcast && !string.IsNullOrEmpty(cache.NotifierName))
            {
                Logging.Log(Logging.LoggingLevel.Minimal, "Broadcasting expire {0}:{1}:{2}", cacheName, cacheKey, itemKey);
                getNotifier(cache.NotifierName).BroadcastExpireCache(cacheName, cacheKey, itemKey);
            }
        }

        private static void onNotifierMessage(string clientId, string message)
        {
            if (Service.OnNotifierMessage != null)
                Service.OnNotifierMessage(clientId, message);
        }

        private static void onCacheExpire(string cacheName, string key)
        {
            expireCacheEntry(cacheName, key, false);
        }
        private static void onCacheItemExpire(string cacheName, string key, string itemKey)
        {
            expireCacheItemEntry(cacheName, key, itemKey, false);
        }

    }
}
