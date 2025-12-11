using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Model;
using Upload.Services.Process;
using Upload.Services.Worker.Implement.JobIplm;
using Upload.Services.Worker.Implement.WorkerPoolIplm;
using WinSCP;

namespace Upload.Common
{
    internal static class ModelUtil
    {

        public static async Task<StoreFileModel> Download(FileModel fileModel, string localDir, string zipPassword)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);

                if (fileModel == null)
                {
                    return null;
                }
                string storePath = Path.Combine(localDir, fileModel.ProgramPath);
                StoreFileModel storeFileModel = new StoreFileModel(fileModel)
                {
                    StorePath = storePath
                };
                if (File.Exists(storePath) && Util.GetMD5HashFromFile(storePath).Equals(fileModel.Md5))
                {
                    return storeFileModel;
                }
                if (await FileProcessSevice.Instance.DownloadFilesAsync(new FileModel[] { fileModel }, localDir, zipPassword))
                {
                    return storeFileModel;
                }
                return null;
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        public static async Task<T> GetModelConfig<T>(string remotePath, string zipPassword)
        {
            return await SftpWorkerPool.Instance.Enqueue(new SftpJob()
            {
                Execute = async (sftp) =>
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
            }).WaitAsync<T>();
        }

        internal static async Task<(bool, string)> UpLoadAppListModel(AppList appList, Location location, string zipPassword)
        {
            string appConfigRemotePath = PathUtil.GetAppConfigRemotePath(location);
            return (await UploadModel(appList, appConfigRemotePath, zipPassword), appConfigRemotePath);
        }

        public static async Task<bool> UploadModel(object model, string remotePath, string zipPassword)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                if (model == null || remotePath == null)
                {
                    return false;
                }
                return await SftpWorkerPool.Instance.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
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
                }).WaitAsync<bool>();
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        public static async Task RemoveRemoteFile(ICollection<FileModel> removeFileModel)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                await FileProcessSevice.Instance.DeleteFilesAsync(removeFileModel);
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }

        }

        internal static async Task<(AppList, string)> GetAppListModel(Location location, string zipPassword)
        {
            string appConfigRemotePath = PathUtil.GetAppConfigRemotePath(location);
            return (await GetModelConfig<AppList>(appConfigRemotePath, zipPassword), appConfigRemotePath);
        }

        internal static async Task<HashSet<FileModel>> GetCanDeleteFileModelsAsync(List<FileModel> fileModels,
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
                appModel = await GetModelConfig<AppModel>(appInfo.Value.AppPath, zipPassword);
                Dictionary<string, HashSet<FileModel>> md5FileGroups = appModel?.FileModels?.GroupBy(f => f.Md5).ToDictionary(g => g.Key, g => new HashSet<FileModel>(g.Select(f => f)));
                canDeleteFileGroups = canDeleteFileGroups.Where(f => !md5FileGroups.ContainsKey(f.Key)).ToDictionary(f => f.Key, f => f.Value);
                if (canDeleteFileGroups.Count == 0) break;
            }
            return canDeleteFileGroups.Values.SelectMany(set => set).Distinct().ToHashSet();
        }
    }
}
