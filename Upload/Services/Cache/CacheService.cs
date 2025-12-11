using AutoDownload.Gui;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Upload.Common;
using static Upload.Common.ConstKey;

namespace Upload.Services.Cache
{
    public sealed class CacheService
    {
        private static readonly Lazy<CacheService> _instance = new Lazy<CacheService>(() => new CacheService());
        public static CacheService Instance => _instance.Value;
        private readonly CacheManagement cacheManager;
        private readonly SemaphoreSlim _lockInitLoad;
        private readonly string _cacheExtension;
        private CacheService()
        {
            _cacheExtension = CACHE_EXTENSION;
            _lockInitLoad = new SemaphoreSlim(100);
            cacheManager = new CacheManagement();
        }

        public string CacheFolder { get; private set; }
        public async Task Init(string cacheFolder)
        {
            if (Directory.Exists(cacheFolder))
            {
                CacheFolder = Path.GetFullPath(cacheFolder);
                cacheManager.Clear();
                foreach (var file in Directory.EnumerateFiles(cacheFolder, "*", SearchOption.AllDirectories))
                {
                    if (!file.EndsWith(_cacheExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        continue;
                    }
                    await _lockInitLoad.WaitAsync();
                    try
                    {
                        string md5 = Util.GetMD5HashFromFile(file);
                        cacheManager.Add(new CacheModel(file, md5));
                    }
                    finally
                    {
                        _lockInitLoad.Release();
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(cacheFolder);
            }
        }
        public bool TryCopyFileTo(string md5, string targetFilePath)
        {
            if (string.IsNullOrWhiteSpace(md5) || string.IsNullOrWhiteSpace(targetFilePath))
            {
                return false;
            }
            if (IsCorrecMd5(targetFilePath, md5))
            {
                return true;
            }
            if (cacheManager.TryGetCache(md5, out var cachedItem))
            {
                try
                {
                    if (IsCorrecMd5(cachedItem.FilePath, cachedItem.MD5))
                    {
                        return Util.CopyFile(cachedItem.FilePath, targetFilePath);
                    }
                    else
                    {
                        cacheManager.Remove(md5);
                    }
                }
                catch (Exception ex)
                {
                    LoggerBox.Addlog($"Cache.Copy, {targetFilePath}: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public bool TryGetCache(string md5, out CacheModel cacheModel)
        {
            cacheModel = default;
            try
            {
                if (cacheManager.TryGetCache(md5, out cacheModel))
                {
                    if (IsCorrecMd5(cacheModel.FilePath, cacheModel.MD5))
                    {
                        return true;
                    }
                    else
                    {
                        cacheManager.Remove(md5);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LoggerBox.Addlog($"Cache.HasMd5, {ex.Message}");
                return false;
            }
        }

        public bool Add(string sourceFile, string md5, out CacheModel newCacheModel)
        {
            newCacheModel = default;
            if (!IsCorrecMd5(sourceFile, md5))
            {
                return false;
            }
            if (cacheManager.Contain(md5))
            {
                return true;
            }
            try
            {
                newCacheModel = new CacheModel(Path.Combine(CacheFolder, md5 + _cacheExtension), md5);
                if (Util.CopyFile(sourceFile, newCacheModel.FilePath))
                {
                    return cacheManager.Add(newCacheModel);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void Remove(string md5)
        {
            cacheManager.Remove(md5);
        }
        public bool RegisterLink(string md5, string appId)
        {
            return cacheManager.RegisterAppId(md5, appId);
        }

        public void UnsubscribeAppId(string appId)
        {
            cacheManager.UnsubscribeAppId(appId);
        }

        public void UnsubscribeAppId(string md5, string appId)
        {
            cacheManager.UnsubscribeAppId(md5, appId);
        }
        public static bool IsCorrecMd5(string filePath, string md5)
        {
            return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath) && Util.GetMD5HashFromFile(filePath) == md5;
        }

    }
}
