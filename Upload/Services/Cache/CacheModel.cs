using System.Collections.Generic;
using System.IO;

namespace Upload.Services.Cache
{
    public class CacheModel
    {
        private readonly HashSet<string> linked;

        public CacheModel(string cachePath, string md5)
        {
            linked = new HashSet<string>();
            FilePath = cachePath;
            MD5 = md5;
        }
        public string MD5 { get; }
        public string FilePath { get; set; }
        public void RemoveAppId(string link)
        {
            linked.Remove(link);
        }
        public void RegisterAppId(string link)
        {
            linked.Add(link);
        }
        public bool IsUseless => linked.Count == 0 || !Exists;
        public bool Exists => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
    }
}
