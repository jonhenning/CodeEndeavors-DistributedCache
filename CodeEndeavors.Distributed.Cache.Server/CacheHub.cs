using CodeEndeavors.Extensions;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Server
{
    [HubName("CacheHub")]
    public class CacheHub : Hub
    {
        public void SendMessage(Dictionary<string, object> args)
        {
            Clients.All.serverMessage(args.GetSetting("clientId", ""), args.GetSetting("message", ""));
        }
        public void ExpireCache(Dictionary<string, object> args)
        {
            Clients.Others.expireCache(args.GetSetting("cacheName", ""), args.GetSetting("key", ""));
        }
    }
}
