using System.IO;
using Upload.Config;

namespace Upload.Common
{
    internal static class PathUtil
    {

        internal static string GetRemotePath()
        {
            return AutoDLConfig.ConfigModel.RemotePath;
        }

        internal static string GetProductPath(Location location)
        {
            return ToSftpPath(Path.Combine(GetRemotePath(), location.Product));
        }

        internal static string GetStationPath(Location location)
        {
            return ToSftpPath(Path.Combine(GetProductPath(location), location.Station));
        }

        internal static string GetProgramFolderPath(Location location)
        {
            return ToSftpPath(Path.Combine(GetStationPath(location), "Program"));
        }

        internal static string GetAppModelPath(Location location)
        {
            return ToSftpPath(Path.Combine(GetProgramFolderPath(location), $"{location.AppName}_AppModel.zip"));
        }

        internal static string GetAppAccessUserPath(Location location)
        {
            return ToSftpPath(Path.Combine(GetProgramFolderPath(location), $"{location.AppName}_AccessUserList.zip"));
        }

        internal static string GetStationAccessUserPath(Location location)
        {
            return ToSftpPath(Path.Combine(GetStationPath(location), "AccessUserList.zip"));
        }

        internal static string GetAppConfigRemotePath(Location location)
        {
            return ToSftpPath(Path.Combine(GetStationPath(location), "Apps.zip"));
        }

        internal static string GetCommonPath(Location location)
        {
            return ToSftpPath(Path.Combine(GetStationPath(location), "Common"));
        }

        internal static string GetUiStoreRemoteFolder()
        {
            return ToSftpPath(Path.Combine(GetRemotePath(), "UiStoreUpdate"));
        }
        internal static string GetUiStoreRemotePath()
        {
            return ToSftpPath(Path.Combine(GetUiStoreRemoteFolder(), "UiStoreModel.zip"));
        }
        internal static string GetUiStoreRemoteCommonPath()
        {
            return ToSftpPath(Path.Combine(GetUiStoreRemoteFolder(), "Common"));
        }
        private static string ToSftpPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            return path.Replace('\\', '/');
        }
    }
}
