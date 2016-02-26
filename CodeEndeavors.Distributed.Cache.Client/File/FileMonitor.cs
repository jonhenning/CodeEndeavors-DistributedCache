using CodeEndeavors.Distributed.Cache.Client.Extensions;
using CodeEndeavors.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client.File
{
    public class FileMonitor : IMonitor 
    {
        private FileSystemWatcher _watcher;

        public string Name { get { return "FileMonitor"; } }

        public string ClientId { get; set; }
        public string CacheName { get; set; }
        public string CacheKey { get; set; }
        public string CacheItemKey { get; set; }

        public event Action<string, string> OnExpire;

        public event Action<string, string, string> OnExpireItem;

        public bool Initialize(string cacheName, string key, dynamic options)
        {
            return Initialize(cacheName, key, null, options);
        }

        public bool Initialize(string cacheName, string key, string itemKey, dynamic options)
        {
            CacheName = cacheName;
            CacheKey = key;
            CacheItemKey = itemKey;

            var optionDict = ((object)options).ToDictionary();
            var fileName = optionDict.GetSetting("fileName", "");

            if (!string.IsNullOrEmpty(fileName))
            {
                var fileInfo = new FileInfo(fileName);
                _watcher = new FileSystemWatcher();
                _watcher.Filter = fileInfo.Name;
                //_watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                _watcher.Path = fileInfo.DirectoryName;
                _watcher.Changed += onChanged;
                _watcher.EnableRaisingEvents = true;
                log(Logging.LoggingLevel.Minimal, "Initialized");
                return true;
            }

            return false;
        }

        private void onChanged(object source, FileSystemEventArgs e)
        {
            log(Logging.LoggingLevel.Minimal, "OnChanged");
            if (string.IsNullOrEmpty(CacheItemKey))
            {
                if (OnExpire != null)
                    OnExpire(CacheName, CacheKey);
            }
            else
            {
                if (OnExpireItem != null)
                    OnExpireItem(CacheName, CacheKey, CacheItemKey);
            }
        }

        public void Dispose()
        {
            if (_watcher != null)
                _watcher.Dispose();
        }

        #region Logging

        protected void log(Logging.LoggingLevel level, string msg)
        {
            log(level, msg, "");
        }
        protected void log(Logging.LoggingLevel level, string msg, params object[] args)
        {
            Logging.Log(level, string.Format("[{0}:{1}:{2}:{3}:{4}] - {5}", Name, ClientId, CacheName, CacheKey, CacheItemKey, string.Format(msg, args)));
        }
        #endregion

    }
}
