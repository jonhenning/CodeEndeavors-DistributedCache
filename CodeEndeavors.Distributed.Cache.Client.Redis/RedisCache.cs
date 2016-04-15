using CodeEndeavors.Extensions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client.Redis
{
    public class RedisCache : ICache
    {
        private ConnectionMultiplexer _multiplexer = null;

        private string _cacheName;
        private Dictionary<string, object> _connection = new Dictionary<string, object>();

        public string ClientId {get ;set; }
        public string Name { get { return "RedisCache"; } }

        public string NotifierName {get;set;}

        public bool Initialize(string cacheName, string clientId, string notifierName, string connection)
        {
            _connection = connection.ToObject<Dictionary<string, object>>();

            ClientId = clientId;
            _cacheName = cacheName;
            var server = _connection.GetSetting("server", "");

            _multiplexer = ConnectionMultiplexer.Connect(server);

            log(Logging.LoggingLevel.Minimal, "Initialized");
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
            var type = _multiplexer.GetDatabase().KeyType(key);
            if (type == RedisType.String)
            {
                var value = _multiplexer.GetDatabase().StringGet(key);
                if (value.HasValue)
                    return value.ToString().ToObject<T>();
            }
            else if (type == RedisType.List)
            {
                var value = _multiplexer.GetDatabase().ListRange(key);
                if (value != null)
                    return value.ToJson().ToObject<T>();    
            }
            else if (type == RedisType.Hash)
            {
                var value = _multiplexer.GetDatabase().HashGetAll(key);
                if (value != null)
                    return value.ToJson().ToObject<T>();    
            }
            else if (type == RedisType.None)
            {
                return defaultValue;
            }
            else
                throw new Exception("RedisType not supported: " + type.ToString());

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

        public void Set<T>(string key, TimeSpan? absoluteExpiration, T value)
        {
            var json = value.ToJson(false, "db");   //todo: this is really hacky that at this level we are imposing the serialization rules from resourcemanager here - db

            if (!absoluteExpiration.HasValue && _connection.ContainsKey("absoluteExpiration"))
                absoluteExpiration = _connection.GetSetting("absoluteExpiration", "'00:00:00'").ToObject<TimeSpan>();

            _multiplexer.GetDatabase().StringSet(key, json, expiry: absoluteExpiration);
        }

        public void ListPush<T>(string key, TimeSpan? absoluteExpiration, T[] values)
        {
            RedisValue[] redisValues = null;
            if (typeof(T) == typeof(string))
                redisValues = Array.ConvertAll(values, item => (RedisValue)item.ToString());
            else
                redisValues = Array.ConvertAll(values, item => (RedisValue)item.ToJson());
            var listExists = _multiplexer.GetDatabase().KeyExists(key);
            _multiplexer.GetDatabase().ListRightPush(key, redisValues);

            if (!absoluteExpiration.HasValue && _connection.ContainsKey("absoluteExpiration"))
                absoluteExpiration = _connection.GetSetting("absoluteExpiration", "'00:00:00'").ToObject<TimeSpan>();
            if (listExists && absoluteExpiration.HasValue)
                _multiplexer.GetDatabase().KeyExpire(key, DateTime.Now.Add(absoluteExpiration.Value));
        }

        /// <summary>
        /// Setting cache entry in hash with potential absoluteExpiration
        /// Expiration applies to entire hash, therefore the expiration will only apply on the initial create of the hash
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Hash key</param>
        /// <param name="itemKey">Hash item key</param>
        /// <param name="absoluteExpiration">Span of time to cache entire hash</param>
        /// <param name="value">value to cache</param>
        public void Set<T>(string key, string itemKey, TimeSpan? absoluteExpiration, T value)
        {
            var json = value.ToJson(false, "db");   //todo: this is really hacky that at this level we are imposing the serialization rules from resourcemanager here - db
            var hashExists = !_multiplexer.GetDatabase().KeyExists(key);
            _multiplexer.GetDatabase().HashSet(key, itemKey, json);

            if (!absoluteExpiration.HasValue && _connection.ContainsKey("absoluteExpiration"))
                absoluteExpiration = _connection.GetSetting("absoluteExpiration", "'00:00:00'").ToObject<TimeSpan>();
            if (hashExists && absoluteExpiration.HasValue)
                _multiplexer.GetDatabase().KeyExpire(key, DateTime.Now.Add(absoluteExpiration.Value));
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
