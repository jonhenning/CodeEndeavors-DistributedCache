using CodeEndeavors.Extensions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace CodeEndeavors.Distributed.Cache.Client.Redis
{
    public class RedisNotifier : INotifier
    {
        private ConnectionMultiplexer _multiplexer = null;

        public event Action<string, string> OnMessage;
        public event Action<string, string> OnExpire;
        public event Action<string, string, string> OnExpireItem;
        public event Action<Exception> OnError;

        public string ClientId { get; set; }
        public string Name { get { return "RedisNotifier"; } }
        
        public bool Initialize(string clientId, string connection)
        {
            var connectionDict = connection.ToObject<Dictionary<string, object>>();

            ClientId = clientId;
            var server = connectionDict.GetSetting("server", "");

            _multiplexer = ConnectionMultiplexer.Connect(server);

            var subscriber = _multiplexer.GetSubscriber();
            subscriber.Subscribe("RedisNotifier.serverMessage", onMessage);
            subscriber.Subscribe("RedisNotifier.expireCache", onExpireCache);
            subscriber.Subscribe("RedisNotifier.expireItemCache", onExpireItemCache);

            BroadcastMessage(string.Format("Redis {0} client connected on: {1}", clientId, server));

            log(Service.LoggingLevel.Minimal, "Initialized");
            return true;
        }

        public void BroadcastMessage(string message)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "message", message } };
            var subscriber = _multiplexer.GetSubscriber();
            subscriber.Publish("RedisNotifier.serverMessage", args.ToJson());
            log(Service.LoggingLevel.Detailed, "Broadcasting Message: " + message);
        }

        public void BroadcastExpireCache(string cacheName, string key)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key } };
            var subscriber = _multiplexer.GetSubscriber();
            subscriber.Publish("RedisNotifier.expireCache", args.ToJson());
            log(Service.LoggingLevel.Detailed, "Broadcasting ExpireCache: {0}", key);
        }
        public void BroadcastExpireCache(string cacheName, string key, string itemKey)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key }, { "itemKey", itemKey } };
            var subscriber = _multiplexer.GetSubscriber();
            subscriber.Publish("RedisNotifier.expireItemCache", args.ToJson());
            log(Service.LoggingLevel.Detailed, "Broadcasting ExpireCache: {0}:{1}", key, itemKey);
        }

        private void onMessage(RedisChannel channel, RedisValue value)
        {
            log(Service.LoggingLevel.Detailed, "Received Message: {0}", value.ToString());

            var dict = value.ToString().ToObject<Dictionary<string, string>>();
            if (OnMessage != null)
                OnMessage(dict["cacheName"], dict["message"]);
        }

        private void onExpireCache(RedisChannel channel, RedisValue value)
        {
            log(Service.LoggingLevel.Detailed, "Received Expire: {0}", value.ToJson());
            var dict = value.ToString().ToObject<Dictionary<string, string>>();
            if (OnExpire != null)
                OnExpire(dict["cacheName"], dict["key"]);
        }
        private void onExpireItemCache(RedisChannel channel, RedisValue value)
        {
            log(Service.LoggingLevel.Detailed, "Received Expire Item: {0}", value.ToJson());

            var dict = value.ToString().ToObject<Dictionary<string, string>>();
            if (OnExpireItem != null)
                OnExpireItem(dict["cacheName"], dict["key"], dict["itemKey"]);
        }

        private void onError(Exception ex)
        {
            log(Service.LoggingLevel.Minimal, "Error: {0}", ex.ToString());

            //todo: expire all caches, we died!
            if (OnError != null)
                OnError(ex);
        }

        public void Dispose()
        {
            if (_multiplexer != null)
            {
                _multiplexer.Dispose();
                log(Service.LoggingLevel.Minimal, "Disposed");
            }
        }

        #region Logging
        public event Action<Service.LoggingLevel, string> OnLoggingMessage;

        protected void log(Service.LoggingLevel level, string msg)
        {
            log(level, msg, "");
        }
        protected void log(Service.LoggingLevel level, string msg, params object[] args)
        {
            if (OnLoggingMessage != null)
                OnLoggingMessage(level, string.Format("[{0}:{1}] - {2}", Name, ClientId, msg.IndexOf("{0}") > -1 ? string.Format(msg, args) : msg));
        }
        #endregion
    }
}
