using System;
using CodeEndeavors.Extensions;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace CodeEndeavors.Distributed.Cache.Client.Web
{
    public class HttpRequestCache : ICache 
    {
        private string _connection;
        private string _cacheName;
        private string _clientId;

        public string ClientId {get;set;}
        public string NotifierName { get; set; }

        private string _webItemCacheKey;

        private Dictionary<string, object> requestDictionary
        {
            get 
            {
                if (System.Web.HttpContext.Current != null)
                {
                    if (!System.Web.HttpContext.Current.Items.Contains(_cacheName))
                        System.Web.HttpContext.Current.Items[_cacheName] = new Dictionary<string, object>();
                    return System.Web.HttpContext.Current.Items[_cacheName] as Dictionary<string, object>;
                }
                return new Dictionary<string, object>();    //no cache at all
            }
        }

        public bool Initialize(string cacheName, string clientId, string notifierName, string connection)
        {
            _connection = connection;
            _cacheName = cacheName;
            _clientId = clientId;
            return true;
        }

        public bool Exists(string key)
        {
            return requestDictionary.ContainsKey(key);
        }
        public bool Exists(string key, string itemKey)
        {
            return getItemDictionary(key).ContainsKey(itemKey);
        }

        public bool GetExists<T>(string key, out T entry)
        {
            entry = default(T);
            if (requestDictionary.ContainsKey(key))
            {
                entry = requestDictionary.GetSetting<T>(key, default(T));
                return true;
            }
            return false;
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
            return requestDictionary.GetSetting(key, defaultValue);
        }

        public T Get<T>(string key, string itemKey, T defaultValue)
        {
            return getItemDictionary(key).GetSetting<T>(itemKey, defaultValue);
        }

        public void Set<T>(string key, T value)
        {
            requestDictionary[key] = value;
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
            return requestDictionary.Remove(key);
        }

        private Dictionary<string, object> getItemDictionary(string key)
        {
            return getItemDictionary(key, false);
        }
        private Dictionary<string, object> getItemDictionary(string key, bool insertIfMissing)
        {
            return requestDictionary.GetSetting(key, new Dictionary<string, object>(), insertIfMissing);
        }

        public void Dispose()
        {
            
        }
    }
}
