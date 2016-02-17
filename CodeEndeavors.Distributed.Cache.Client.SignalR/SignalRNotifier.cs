using CodeEndeavors.Extensions;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client.SignalR
{
    public class SignalRNotifier : INotifier
    {
        private HubConnection _hubConnection = null;
        private IHubProxy _proxy = null;

        public event Action<string, string> OnMessage;
        public event Action<string, string> OnExpire;
        public event Action<string, string, string> OnExpireItem;
        public event Action<Exception> OnError;

        public string ClientId { get; set; }
        
        public bool Initialize(string clientId, string connection)
        {
            var connectionDict = connection.ToObject<Dictionary<string, object>>();

            ClientId = clientId;
            var url = connectionDict.GetSetting("url", "");

            _hubConnection = new HubConnection(url);
            _proxy = _hubConnection.CreateHubProxy("CacheHub");
            _proxy.On<string, string>("serverMessage", onMessage);
            _proxy.On<string, string>("expireCache", onExpireCache);
            _proxy.On<string, string, string>("expireItemCache", onExpireItemCache);
            _hubConnection.Error += onError;
            _hubConnection.ConnectionSlow += onConnectionSlow;

            _hubConnection.Start().Wait();

            BroadcastMessage(string.Format("SignalR {0} client connected on: {1}", clientId, url));

            return true;
        }

        public void BroadcastMessage(string message)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "message", message } };
            _proxy.Invoke<Dictionary<string, object>>("SendMessage", args);
        }

        public void BroadcastExpireCache(string cacheName, string key)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key } };
            _proxy.Invoke<Dictionary<string, object>>("ExpireCache", args);
        }
        public void BroadcastExpireCache(string cacheName, string key, string itemKey)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key }, { "itemKey", itemKey } };
            _proxy.Invoke<Dictionary<string, object>>("ExpireItemCache", args);
        }

        private void onMessage(string clientId, string message)
        {
            if (OnMessage != null)
                OnMessage(clientId, message);
        }

        private void onExpireCache(string cacheName, string key)
        {
            if (OnExpire != null)
                OnExpire(cacheName, key);
        }
        private void onExpireItemCache(string cacheName, string key, string itemKey)
        {
            if (OnExpireItem != null)
                OnExpireItem(cacheName, key, itemKey);
        }

        private void onError(Exception ex)
        {
            //todo: expire all caches, we died!
            if (OnError != null)
                OnError(ex);
        }
        private void onConnectionSlow()
        {
            onMessage(ClientId, "Connection Slow...");
        }

        public void Dispose()
        {
            if (_hubConnection != null)
                _hubConnection.Dispose();
        }
    }
}
