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

            return true;
        }

        public void BroadcastMessage(string message)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "message", message } };
            var subscriber = _multiplexer.GetSubscriber();
            subscriber.Publish("RedisNotifier.serverMessage", args.ToJson());
        }

        public void BroadcastExpireCache(string cacheName, string key)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key } };
            var subscriber = _multiplexer.GetSubscriber();
            subscriber.Publish("RedisNotifier.expireCache", args.ToJson());
        }
        public void BroadcastExpireCache(string cacheName, string key, string itemKey)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key }, { "itemKey", itemKey } };
            var subscriber = _multiplexer.GetSubscriber();
            subscriber.Publish("RedisNotifier.expireItemCache", args.ToJson());
        }

        private void onMessage(RedisChannel channel, RedisValue value)
        {
            var dict = value.ToString().ToObject<Dictionary<string, string>>();
            if (OnMessage != null)
                OnMessage(dict["cacheName"], dict["message"]);
        }

        private void onExpireCache(RedisChannel channel, RedisValue value)
        {
            var dict = value.ToString().ToObject<Dictionary<string, string>>();
            if (OnExpire != null)
                OnExpire(dict["cacheName"], dict["key"]);
        }
        private void onExpireItemCache(RedisChannel channel, RedisValue value)
        {
            var dict = value.ToString().ToObject<Dictionary<string, string>>();
            if (OnExpireItem != null)
                OnExpireItem(dict["cacheName"], dict["key"], dict["itemKey"]);
        }

        private void onError(Exception ex)
        {
            //todo: expire all caches, we died!
            if (OnError != null)
                OnError(ex);
        }

        public void Dispose()
        {
            if (_multiplexer != null)
                _multiplexer.Dispose();
        }
    }
}
