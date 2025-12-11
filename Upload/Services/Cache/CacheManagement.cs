using AutoDownload.Gui;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Upload.Services.Cache
{
    public class CacheManagement
    {
        private readonly ConcurrentDictionary<string, CacheModel> _cacheModels;

        public CacheManagement()
        {
            _cacheModels = new ConcurrentDictionary<string, CacheModel>();
        }
        public bool TryGetCache(string md5, out CacheModel itemInfo)
        {
            itemInfo = default;
            if (string.IsNullOrWhiteSpace(md5))
            {
                return false;
            }
            try
            {
                return _cacheModels.TryGetValue(md5, out itemInfo) && itemInfo.MD5 == md5;
            }
            catch (Exception ex)
            {
                LoggerBox.Addlog(ex.Message);
                return false;
            }
        }
        public bool Add(CacheModel cacheModel)
        {
            lock (_cacheModels)
            {
                if (cacheModel == null ||
                string.IsNullOrWhiteSpace(cacheModel.MD5) ||
                string.IsNullOrWhiteSpace(cacheModel.FilePath))
                {
                    return false;
                }
                return _cacheModels.TryAdd(cacheModel.MD5, cacheModel);
            }
        }
        public bool Contain(string md5)
        {
            return !string.IsNullOrWhiteSpace(md5) && _cacheModels.ContainsKey(md5);
        }
        public bool RegisterAppId(string md5, string appId)
        {
            if (string.IsNullOrWhiteSpace(md5))
            {
                return false;
            }
            lock (_cacheModels)
            {
                if (TryGetCache(md5, out var cache))
                {
                    cache.RegisterAppId(appId);
                    return true;
                }
                return false;
            }
        }

        public void UnsubscribeAppId(string md5, string appId)
        {
            if (string.IsNullOrWhiteSpace(md5))
            {
                return;
            }
            lock (_cacheModels)
            {
                if (_cacheModels.TryGetValue(md5, out var cacheModel))
                {
                    cacheModel.RemoveAppId(appId);
                    if (cacheModel.IsUseless)
                    {
                        _cacheModels.TryRemove(md5, out _);
                    }
                }
            }
        }

        public void UnsubscribeAppId(string appId)
        {
            if (string.IsNullOrEmpty(appId))
            {
                return;
            }
            lock (_cacheModels)
            {
                _cacheModels.Where(i =>
                {
                    i.Value.RemoveAppId(appId);
                    return i.Value.IsUseless;
                }).ToList().ForEach(i => Remove(i.Key));
            }
        }

        public void Remove(string md5)
        {
            if (string.IsNullOrWhiteSpace(md5))
            {
                return;
            }
            if (_cacheModels.TryRemove(md5, out var cacheItem))
            {
                if (File.Exists(cacheItem.FilePath))
                {
                    File.Delete(cacheItem.FilePath);
                }
            }
        }

        public void Clear()
        {
            _cacheModels.Clear();
        }
    }
}
