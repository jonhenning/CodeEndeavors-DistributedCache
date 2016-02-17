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
                return true;
            }

            return false;
        }
        
        private void onChanged(object source, FileSystemEventArgs e)
        {
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

        public string ClientId { get; set; }
        public string CacheName { get; set; }
        public string CacheKey { get; set; }
        public string CacheItemKey { get; set; }

        public event Action<string, string> OnExpire;

        public event Action<string, string, string> OnExpireItem;

        public void Dispose()
        {
            if (_watcher != null)
                _watcher.Dispose();
        }
    }
}
