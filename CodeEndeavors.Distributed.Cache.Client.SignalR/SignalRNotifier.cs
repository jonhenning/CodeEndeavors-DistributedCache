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
        public string Name { get { return "SignalRNotifier"; } }

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

            log(Service.LoggingLevel.Minimal, "Initialized");
            return true;
        }

        public void BroadcastMessage(string message)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "message", message } };
            _proxy.Invoke<Dictionary<string, object>>("SendMessage", args);
            log(Service.LoggingLevel.Detailed, "Broadcasting Message: " + message);
        }

        public void BroadcastExpireCache(string cacheName, string key)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key } };
            _proxy.Invoke<Dictionary<string, object>>("ExpireCache", args);
            log(Service.LoggingLevel.Detailed, "Broadcasting ExpireCache: {0}", key);
        }
        public void BroadcastExpireCache(string cacheName, string key, string itemKey)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", cacheName }, { "key", key }, { "itemKey", itemKey } };
            _proxy.Invoke<Dictionary<string, object>>("ExpireItemCache", args);
            log(Service.LoggingLevel.Detailed, "Broadcasting ExpireCache: {0}:{1}", key, itemKey);
        }

        private void onMessage(string clientId, string message)
        {
            log(Service.LoggingLevel.Detailed, "Received Message: {0}", message);
            if (OnMessage != null)
                OnMessage(clientId, message);
        }

        private void onExpireCache(string cacheName, string key)
        {
            log(Service.LoggingLevel.Detailed, "Received Expire: {0}", key);
            if (OnExpire != null)
                OnExpire(cacheName, key);
        }
        private void onExpireItemCache(string cacheName, string key, string itemKey)
        {
            log(Service.LoggingLevel.Detailed, "Received Expire Item: {0}:{1}", key, itemKey);
            if (OnExpireItem != null)
                OnExpireItem(cacheName, key, itemKey);
        }

        private void onError(Exception ex)
        {
            log(Service.LoggingLevel.Minimal, "Error: {0}", ex.ToString());
            //todo: expire all caches, we died!
            if (OnError != null)
                OnError(ex);
        }
        private void onConnectionSlow()
        {
            log(Service.LoggingLevel.Detailed, "Connection Slow");
            onMessage(ClientId, "Connection Slow...");
        }

        public void Dispose()
        {
            if (_hubConnection != null)
            {
                _hubConnection.Dispose();
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
                OnLoggingMessage(level, string.Format("[{0}:{1}] - {2}", Name, ClientId, string.Format(msg, args)));
        }
        #endregion
    }
}
