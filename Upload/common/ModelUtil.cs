using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Model;
using Upload.Services.Sftp;

namespace Upload.Common
{
    internal static class ModelUtil
    {

        public static async Task<T> GetModelConfig<T>(ISftpClient sftp, string remotePath, string zipPassword)
        {

            if (!await sftp.Exists(remotePath))
            {
                return default;
            }
            try
            {
                using (var zipStream = await sftp.DownloadFileToStreamAsync(remotePath))
                {
                    if (zipStream == null)
                    {
                        return default;
                    }
                    string appConfig = ZipHelper.ExtractToJsonString(zipStream, zipPassword);
                    if (string.IsNullOrEmpty(appConfig))
                    {
                        return default;
                    }
                    var result = JsonConvert.DeserializeObject<T>(appConfig);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(remotePath, ex);
            }
        }

        internal static async Task<(bool, string)> UpLoadAppListModel(ISftpClient sftp, AppList appList, Location location, string zipPassword)
        {
            string appConfigRemotePath = PathUtil.GetAppConfigRemotePath(location);
            return (await UploadModel(sftp, appList, appConfigRemotePath, zipPassword), appConfigRemotePath);
        }

        public static async Task<bool> UploadModel(ISftpClient sftp, object model, string remotePath, string zipPassword)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                if (model == null || remotePath == null)
                {
                    return false;
                }
                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                using (var zipStream = await ZipHelper.JsonAsZipToStream(json, Path.GetFileNameWithoutExtension(remotePath), zipPassword))
                {
                    if (!await sftp.CreateDirectory(Path.GetDirectoryName(remotePath)))
                    {
                        return false;
                    }
                    return await sftp.UploadStreamFileAsync(zipStream, remotePath);
                }
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        internal static async Task<(AppList, string)> GetAppListModel(ISftpClient sftp, Location location, string zipPassword)
        {
            string appConfigRemotePath = PathUtil.GetAppConfigRemotePath(location);
            return (await GetModelConfig<AppList>(sftp, appConfigRemotePath, zipPassword), appConfigRemotePath);
        }

        internal static async Task<List<FileModel>> GetCanDeleteFileModelsAsync(ISftpClient sftp, List<FileModel> fileModels,
            AppList appList, string zipPassword, string thisAppPath = null)
        {
            Dictionary<string, HashSet<FileModel>> canDeleteFileGroups = fileModels.GroupBy(f => f.Md5).ToDictionary(g => g.Key, g => new HashSet<FileModel>(g.Select(f => f)));
            var fileModelUsed = new Dictionary<string, FileModel>();
            AppModel appModel = null;
            foreach (var appInfo in appList.ProgramPaths)
            {
                if (!string.IsNullOrEmpty(thisAppPath) && thisAppPath == appInfo.Value.AppPath)
                {
                    continue;
                }
                appModel = await GetModelConfig<AppModel>(sftp, appInfo.Value.AppPath, zipPassword);
                Dictionary<string, HashSet<FileModel>> md5FileGroups = appModel?.FileModels?.GroupBy(f => f.Md5).ToDictionary(g => g.Key, g => new HashSet<FileModel>(g.Select(f => f)));
                canDeleteFileGroups = canDeleteFileGroups.Where(f => !md5FileGroups.ContainsKey(f.Key)).ToDictionary(f => f.Key, f => f.Value);
                if (canDeleteFileGroups.Count == 0) break;
            }
            return canDeleteFileGroups.Values.SelectMany(set => set).Distinct().ToList();
        }
    }
}
