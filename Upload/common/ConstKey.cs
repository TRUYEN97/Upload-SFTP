
namespace Upload.Common
{
    internal static class ConstKey
    {
        public static readonly string ZIP_PASSWORD = Util.GetMD5HashFromString("@RaspberryPi5@");
        public static readonly string CACHE_EXTENSION = ".cache";
    }
}
