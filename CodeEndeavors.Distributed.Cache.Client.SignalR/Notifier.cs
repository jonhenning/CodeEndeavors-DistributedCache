using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client.SignalR
{
    public class Notifier
    {
        private static ConcurrentDictionary<string, SignalRNotifier> _signalRNotifiers = new ConcurrentDictionary<string, SignalRNotifier>();

        public static SignalRNotifier GetNotifier(string cacheName, string clientId, string connection)
        {
            if (!_signalRNotifiers.ContainsKey(connection))
            {
                _signalRNotifiers[connection] = new SignalRNotifier(cacheName, clientId, connection);
            }
            return _signalRNotifiers[connection];
        }


    }
}
