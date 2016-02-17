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
    public class RemoteCache : ICache
    {
        private IHubProxy _proxy = null;
        private HubConnection _hubConnection = null;
        private string _url = null;
        private string _cacheName = null;

        public string ClientId { get; set; }
        public bool Initialize(string cacheName, string clientId, string connection, Action<string, string> onExpireCache, Action<string, string, string> onMessage, Action<Exception> onError)
        {
            var connectionDict = connection.ToObject<Dictionary<string, object>>();

            _cacheName = cacheName;
            ClientId = clientId;
            _url = connectionDict.GetSetting("url", "");

            _hubConnection = new HubConnection(_url);
            _proxy = _hubConnection.CreateHubProxy("CacheHub");
            _proxy.On<string, string, string>("serverMessage", (cid, clientName, message) =>
            {
                Debug.WriteLine(message);
                if (onMessage != null)
                    onMessage(cid, clientName, message);
            });

            _proxy.On<string, string>("expireCache", onExpireCache);

            _hubConnection.ConnectionSlow += () =>
            {
                Debug.WriteLine("Connection problems...");
            };
            _hubConnection.Error += onError;

            _hubConnection.Start().Wait();
            
            return true;
        }

        public void SendMessage(string message)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", _cacheName }, { "message", message } };
            _proxy.Invoke<Dictionary<string, object>>("SendMessage", args);
        }

        public void ExpireCache(string key)
        {
            var args = new Dictionary<string, object>() { { "clientId", ClientId }, { "cacheName", _cacheName }, { "key", key } };
            _proxy.Invoke<Dictionary<string, object>>("ExpireCache", args);
        }


    }
}
