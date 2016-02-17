using CodeEndeavors.Extensions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client.Redis
{
    public class RedisCache : ICache
    {
        private ConnectionMultiplexer _multiplexer = null;

        public string ClientId {get ;set; }

        public string NotifierName {get;set;}

        public bool Initialize(string cacheName, string clientId, string notifierName, string connection)
        {
            var connectionDict = connection.ToObject<Dictionary<string, object>>();

            ClientId = clientId;
            var server = connectionDict.GetSetting("server", "");

            _multiplexer = ConnectionMultiplexer.Connect(server);

            return true;
        }

        public bool Exists(string key)
        {
            return _multiplexer.GetDatabase().KeyExists(key);
        }
        public bool Exists(string key, string itemKey)
        {
            //todo: is this the most efficient?
            return _multiplexer.GetDatabase().HashKeys(key).ToList().Exists(h => h == itemKey);
        }

        public T Get<T>(string key, T defaultValue)
        {
            var value = _multiplexer.GetDatabase().StringGet(key);
            if (value.HasValue)
                return value.ToString().ToObject<T>();
            return defaultValue;
        }

        public T Get<T>(string key, string itemKey, T defaultValue)
        {
            var value = _multiplexer.GetDatabase().HashGet(key, itemKey);
            if (value.HasValue)
                return value.ToString().ToObject<T>();
            return defaultValue;
        }

        public bool GetExists<T>(string key, out T entry)
        {
            entry = default(T);
            var value = _multiplexer.GetDatabase().StringGet(key);
            if (value.HasValue)
            {
                entry = value.ToString().ToObject<T>();
                return true;
            }
            return false;
        }

        public bool GetExists<T>(string key, string itemKey, out T entry)
        {
            entry = default(T);
            var value = _multiplexer.GetDatabase().HashGet(key, itemKey);
            if (value.HasValue)
            {
                entry = value.ToString().ToObject<T>();
                return true;
            }
            return false;
        }

        public void Set<T>(string key, T value)
        {
            var json = value.ToJson();
            _multiplexer.GetDatabase().StringSet(key, json);
        }
        public void Set<T>(string key, string itemKey, T value)
        {
            var json = value.ToJson();
            _multiplexer.GetDatabase().HashSet(key, itemKey, json);
        }

        public bool Remove(string key)
        {
            return _multiplexer.GetDatabase().KeyDelete(key);
        }
        public bool Remove(string key, string itemKey)
        {
            return _multiplexer.GetDatabase().HashDelete(key, itemKey);
        }

        public void Dispose()
        {
            if (_multiplexer != null)
                _multiplexer.Dispose();
        }
    }
}
