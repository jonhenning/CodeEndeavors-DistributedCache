using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client
{
    public interface INotifier:  IDisposable
    {
        bool Initialize(string clientId, string connection);

        void BroadcastMessage(string message);
        void BroadcastExpireCache(string cacheName, string key);
        void BroadcastExpireCache(string cacheName, string key, string itemKey);

        string ClientId { get; set; }
        string Name { get; }

        event Action<Service.LoggingLevel, string> OnLoggingMessage;
        event Action<string, string> OnMessage;
        event Action<string, string> OnExpire;
        event Action<string, string, string> OnExpireItem;
        event Action<Exception> OnError;

    }
}
