using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client.SignalR
{
    public class SignalRChangeMonitor : ChangeMonitor, IChangeMonitor
    {
        private string _uniqueId;
        private string _cacheName;
        private string _key;

        public void Initialize(string cacheName, string notifierName, string key)
        {
            this._cacheName = cacheName;
            this._key = key;
            this._uniqueId = Guid.NewGuid().ToString();

            var notifier = Service.GetNotifier(notifierName);
            notifier.OnExpire += OnExpire;

            base.InitializationComplete();
        }

        protected override void Dispose(bool disposing)
        {
            // always Unsubscribe on dispose
            //this.Unsubscribe();
        }

        public override string UniqueId
        {
            get { return this._uniqueId; }
        }

        public void OnExpire(string cacheName, string key)
        {
            if (cacheName == _cacheName && key == _key)
                base.OnChanged(null);
        }

        //private void Unsubscribe()
        //{
        //    if (this.unsubscriber != null)
        //        this.unsubscriber.Dispose();
        //}
    }
}
