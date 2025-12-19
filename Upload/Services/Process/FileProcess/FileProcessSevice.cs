using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Common;
using Upload.gui;
using Upload.Model;
using Upload.Services.Cache;
using Upload.Services.Process.Interface;
using Upload.Services.Worker.Implement.JobIplm;
using Upload.Services.Worker.Implement.WorkerPoolIplm;

namespace Upload.Services.Process
{
    public static class FileProcessSevice
    {
        private static readonly SftpWorkerPool sftpWorkerPool = SftpWorkerPool.Instance;
        private static readonly CacheService cacheService = CacheService.Instance;
        public static async Task<bool> UploadFilesAsync(ICollection<StoreFileModel> fileModels, string zipPassword)
        {
            return await ExecuteFilesAsync(fileModels, (model) => CreateUploadJob(model, zipPassword));
        }

        public static async Task<bool> DeleteFilesAsync(ICollection<FileModel> fileModels)
        {
            return await ExecuteFilesAsync(fileModels, (model) => CreateDeleteJob(model));
        }

        public static async Task<bool> DownloadFilesAsync(ICollection<FileModel> fileModels, string dirPath, string zipPassword)
        {
            return await ExecuteFilesAsync(fileModels, (model) => CreateDownloadJob(dirPath, model, zipPassword));
        }

        public static async Task<bool> DownloadFileAsync(FileModel fileModel, string dirPath, string zipPassword)
        {
            if (fileModel == null || dirPath == null)
            {
                return true;
            }
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                BlockingCollection<IProcessSignal> processSignals = new BlockingCollection<IProcessSignal>();
                FileResultModel model = await sftpWorkerPool.Enqueue(CreateDownloadJob(dirPath, fileModel, zipPassword)).WaitAsync<FileResultModel>();
                Util.ShowMessager(model.Message);
                return model.IsSuccess;
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        private static SftpJob CreateDeleteJob(FileModel model)
        {
            return new SftpJob
            {
                Execute = async (sftp) =>
                {
                    var rs = new FileResultModel(model.ProgramPath, model.RemotePath);
                    try
                    {
                        if (await sftp.Exists(model.RemotePath) && !await sftp.DeleteFile(model.RemotePath))
                        {
                            rs.SetResult(false, $"Delete: {model.ProgramPath} failed!");
                            return rs;
                        }
                        rs.SetResult(true, $"Delete: {model.ProgramPath} ok");
                        return rs;
                    }
                    catch (Exception ex)
                    {
                        rs.SetResult(false, $"{ex.Message}: Delete -> {model.RemotePath}");
                        return rs;
                    }
                }
            };
        }

        private static SftpJob CreateDownloadJob(string dirPath, FileModel model, string zipPassword)
        {
            return new SftpJob
            {
                Execute = async (sftp) =>
                {
                    var rs = new FileResultModel(model.ProgramPath, model.RemotePath);
                    if (dirPath == null)
                    {
                        rs.SetResult(false, "download folder == null!");
                        return rs;
                    }
                    if (!Directory.Exists(dirPath) && !Directory.CreateDirectory(dirPath).Exists)
                    {
                        rs.SetResult(false, $"download folder is not exists: {model.ProgramPath}");
                        return rs;
                    }
                    string storePath = Path.Combine(dirPath, model.ProgramPath);
                    if (cacheService.TryCopyFileTo(model.Md5, storePath))
                    {
                        rs.SetResult(true, $"Copy from Cache -> {storePath}");
                        return rs;
                    }
                    else
                    {
                        if (!sftp.IsConnected)
                        {
                            rs.SetResult(false, $"Cannot connect to SFTP server: {model.ProgramPath}");
                            return rs;
                        }
                        try
                        {

                            using (Stream stream = await sftp.DownloadFileToStreamAsync(model.RemotePath))
                            {
                                if (ZipHelper.ExtractSingleFileFromStream(stream, storePath, zipPassword) && cacheService.Add(storePath, model.Md5, out var _))
                                {
                                    rs.SetResult(true, $"Download: {model.ProgramPath} -> {model.RemotePath} ok");
                                }
                                else
                                {
                                    rs.SetResult(false, $"Download: {model.ProgramPath} -> {model.RemotePath} failed!");
                                }
                            }
                            return rs;
                        }
                        catch (Exception ex)
                        {
                            rs.SetResult(false, $"{ex.Message}: {model.ProgramPath} -> {model.RemotePath}");
                            return rs;
                        }
                    }
                }
            };
        }

        private static SftpJob CreateUploadJob(StoreFileModel model, string zipPassword)
        {
            return new SftpJob
            {
                Execute = async (sftp) =>
                {
                    var rs = new FileResultModel(model.ProgramPath, model.RemotePath);
                    try
                    {
                        model.Md5 = Util.GetMD5HashFromFile(model.StorePath);
                        model.RemotePath = Path.Combine(model.RemoteDir, $"{model.Md5}.zip");
                        if (!cacheService.TryGetCache(model.Md5, out var cacheModel)
                        && !cacheService.Add(model.StorePath, model.Md5, out cacheModel))
                        {
                            rs.SetResult(false, $"Add file({model.ProgramPath}) to Cache failed!");
                            return rs;
                        }
                        if (!FileSizeConverter.TryGetMb(cacheModel.FilePath, out double mb))
                        {
                            rs.SetResult(false, $"Cache file({model.ProgramPath}: {cacheModel.FilePath}) is not exist!");
                            return rs;
                        }
                        model.Mb = mb;
                        if (!sftp.IsConnected)
                        {
                            rs.SetResult(false, $"Cannot connect to SFTP server: {model.ProgramPath}");
                            return rs;
                        }
                        if (await sftp.Exists(model.RemotePath))
                        {
                            rs.SetResult(true, $"Upload: {model.ProgramPath} -> {model.RemotePath} has exists");
                        }
                        else
                        {
                            using (Stream stream = ZipHelper.ZipSingleFiletoStream(model.RemotePath, cacheModel.FilePath, zipPassword))
                            {
                                if (await sftp.UploadStreamFileAsync(stream, model.RemotePath))
                                {
                                    rs.SetResult(true, $"Upload: {model.ProgramPath} -> {model.RemotePath} ok");
                                }
                                else
                                {
                                    rs.SetResult(false, $"Upload: {model.ProgramPath} -> {model.RemotePath} failed!");
                                }
                            }
                        }
                        return rs;
                    }
                    catch (Exception ex)
                    {
                        rs.SetResult(false, $"{ex.Message}: {model.ProgramPath} -> {model.RemotePath}");
                        return rs;
                    }
                }
            };
        }

        private static async Task<bool> ExecuteFilesAsync<T>(ICollection<T> fileModels, Func<T, SftpJob> createJob)
        {
            if (fileModels == null || fileModels.Count == 0)
            {
                return true;
            }
            try
            {
                int count = 0;
                using (ProgressDialogForm form = new ProgressDialogForm("Running...")
                {
                    Maximum = fileModels.Count
                })
                {
                    await form.DoworkAsync(async (report, tk) =>
                    {
                        BlockingCollection<IProcessSignal> processSignals = new BlockingCollection<IProcessSignal>();
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                foreach (var model in fileModels)
                                {
                                    if (tk.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                    SftpJob job = createJob.Invoke(model);
                                    processSignals.Add(sftpWorkerPool.Enqueue(job));
                                }
                            }
                            finally
                            {
                                processSignals.CompleteAdding();
                            }
                        });
                        await Task.Run(async () =>
                        {
                            try
                            {
                                var removeSignals = new List<IProcessSignal>();
                                foreach (IProcessSignal signal in processSignals.GetConsumingEnumerable())
                                {
                                    FileResultModel model = await signal.WaitAsync<FileResultModel>();
                                    if (model.IsSuccess)
                                    {
                                        report.Invoke(++count, model.LocalPath);
                                    }
                                    else
                                    {
                                        Util.ShowMessager(model.Message);
                                    }
                                }
                            }
                            catch (OperationCanceledException) { }
                        });
                    });
                }
                return fileModels.Count == count;
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        private class FileResultModel
        {
            public FileResultModel(string localPath, string storePath)
            {
                LocalPath = localPath;
                RemotePath = storePath;
            }
            public string LocalPath { get; }
            public string RemotePath { get; }
            public string Message { get; private set; }
            public bool IsSuccess { get; private set; }
            public void SetResult(bool status, string mess = null)
            {
                Message = mess;
                IsSuccess = status;
            }
        }
    }
}
