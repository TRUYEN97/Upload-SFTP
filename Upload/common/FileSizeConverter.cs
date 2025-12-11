using System.IO;

namespace Upload.Common
{
    public static class FileSizeConverter
    {
        public static long GetByte(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return -1;
            }
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists ? fileInfo.Length : -1;
        }
        public static bool TryGetByte(string filePath, out long size)
        {
            size = GetByte(filePath);
            return size >= 0;
        }
        public static bool TryGetKb(string filePath, out double size)
        {
            size = GetKb(filePath);
            return size >= 0;
        }
        public static bool TryGetMb(string filePath, out double size)
        {
            size = GetMb(filePath);
            return size >= 0;
        }
        public static double GetKb(string filePath)
        {
            var length = GetByte(filePath);
            if (length < 0)
            {
                return -1;
            }
            return length / 1024.0;
        }
        public static double GetMb(string filePath)
        {
            var length = GetKb(filePath);
            if (length < 0)
            {
                return -1;
            }
            return length / 1024.0;
        }
        public static double GetGb(string filePath)
        {
            var length = GetMb(filePath);
            if (length < 0)
            {
                return -1;
            }
            return length / 1024.0;
        }
    }
}
