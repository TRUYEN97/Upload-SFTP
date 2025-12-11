using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    public sealed class FileProcessSevice
    {
        private static readonly Lazy<FileProcessSevice> _insatance = new Lazy<FileProcessSevice>(() => new FileProcessSevice());
        private readonly SftpWorkerPool sftpWorkerPool;
        private readonly CacheService cacheService;
        private FileProcessSevice()
        {
            sftpWorkerPool = SftpWorkerPool.Instance;
            cacheService = CacheService.Instance;
        }
        public static FileProcessSevice Instance => _insatance.Value;

        public async Task<bool> UploadFilesAsync(ICollection<StoreFileModel> fileModels, string zipPassword)
        {
            if (fileModels == null || fileModels.Count == 0)
            {
                return true;
            }
            try
            {
                int count = 0;
                using (ProgressDialogForm form = new ProgressDialogForm("Upload files")
                {
                    Maximum = fileModels.Count
                })
                {
                    await form.DoworkAsync(async (report, tk) =>
                    {
                        BlockingCollection<IProcessSignal> processSignals = new BlockingCollection<IProcessSignal>();
                        _ = EnqueueUploadStoreFile(fileModels, processSignals, tk, zipPassword);
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
                                    Util.ShowMessager(model.Message);
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
        public async Task<bool> DeleteFilesAsync(ICollection<FileModel> fileModels)
        {
            if (fileModels == null || fileModels.Count == 0)
            {
                return true;
            }
            try
            {
                int count = 0;
                using (ProgressDialogForm form = new ProgressDialogForm("Delete files")
                {
                    Maximum = fileModels.Count
                })
                {
                    await form.DoworkAsync(async (report, tk) =>
                    {
                        BlockingCollection<IProcessSignal> processSignals = new BlockingCollection<IProcessSignal>();
                        _ = EnqueueDeleteRemoteFile(fileModels, processSignals, tk);
                        await Task.Run(async () =>
                        {
                            try
                            {
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

        public async Task<bool> DownloadFilesAsync(ICollection<FileModel> fileModels, string dir, string zipPassword)
        {
            if (fileModels == null || fileModels.Count == 0)
            {
                return true;
            }
            try
            {
                int count = 0;
                using (ProgressDialogForm form = new ProgressDialogForm("Download files")
                {
                    Maximum = fileModels.Count
                })
                {
                    await form.DoworkAsync(async (report, tk) =>
                    {
                        BlockingCollection<IProcessSignal> processSignals = new BlockingCollection<IProcessSignal>();
                        _ = EnqueueDownloadStoreFile(fileModels, processSignals, tk, dir, zipPassword);
                        await Task.Run(async () =>
                        {
                            try
                            {
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

        private async Task EnqueueDeleteRemoteFile(ICollection<FileModel> fileModels, BlockingCollection<IProcessSignal> processSignals, CancellationToken tk)
        {
            await Task.Run(() =>
            {
                foreach (var model in fileModels)
                {
                    if (tk.IsCancellationRequested)
                    {
                        return;
                    }
                    var job = new SftpJob
                    {
                        Execute = async (sftp) =>
                        {
                            var rs = new FileResultModel(model.ProgramPath, model.RemotePath);
                            if (tk.IsCancellationRequested)
                            {
                                rs.SetResult(false, $"Delete canceled!: {model.ProgramPath}");
                                return rs;
                            }
                            try
                            {
                                if (await sftp.Exists(model.RemotePath) && !await sftp.DeleteFile(model.RemotePath))
                                {
                                    rs.SetResult(false, $"Delete: {model.ProgramPath} failed!");
                                    return rs;
                                }
                                rs.SetResult(false, $"Delete: {model.ProgramPath} ok");
                                return rs;
                            }
                            catch (Exception ex)
                            {
                                rs.SetResult(false, $"{ex.Message}: Delete -> {model.RemotePath}");
                                return rs;
                            }
                        }
                    };
                    processSignals.Add(sftpWorkerPool.Enqueue(job));
                }
                processSignals.CompleteAdding();
            });
        }
        private async Task EnqueueDownloadStoreFile(ICollection<FileModel> fileModels,
            BlockingCollection<IProcessSignal> processSignals,
            CancellationToken tk,
            string dirPath, string zipPassword)
        {
            await Task.Run(() =>
            {
                foreach (var model in fileModels)
                {
                    if (tk.IsCancellationRequested)
                    {
                        return;
                    }
                    var job = new SftpJob
                    {
                        Execute = async (sftp) =>
                        {
                            var rs = new FileResultModel(model.ProgramPath, model.RemotePath);
                            if (tk.IsCancellationRequested)
                            {
                                rs.SetResult(false, $"Canceled!: {model.ProgramPath}");
                                return rs;
                            }
                            if (dirPath == null)
                            {
                                rs.SetResult(false, "download folder == null!");
                                return rs;
                            }
                            if (!Directory.Exists(dirPath))
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
                                if (tk.IsCancellationRequested)
                                {
                                    rs.SetResult(false, $"Canceled!: {model.ProgramPath}");
                                    return rs;
                                }
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
                    processSignals.Add(sftpWorkerPool.Enqueue(job));
                }
                processSignals.CompleteAdding();
            });
        }

        private async Task EnqueueUploadStoreFile(ICollection<StoreFileModel> fileModels,
            BlockingCollection<IProcessSignal> processSignals,
            CancellationToken tk, string zipPassword)
        {
            await Task.Run(() =>
            {
                foreach (var model in fileModels)
                {
                    if (tk.IsCancellationRequested)
                    {
                        return;
                    }
                    var job = new SftpJob
                    {
                        Execute = async (sftp) =>
                        {
                            var rs = new FileResultModel(model.ProgramPath, model.RemotePath);
                            if (tk.IsCancellationRequested)
                            {
                                rs.SetResult(false, $"Canceled!: {model.ProgramPath}");
                                return false;
                            }
                            model.Md5 = Util.GetMD5HashFromFile(model.StorePath);
                            model.RemotePath = Path.Combine(model.RemoteDir, $"{model.Md5}.zip");
                            if (!cacheService.TryGetCache(model.Md5, out var cacheModel)
                            && !cacheService.Add(model.StorePath, model.Md5, out cacheModel))
                            {
                                rs.SetResult(false, $"Add file({model.ProgramPath}) to Cache failed!");
                                return false;
                            }
                            if (!FileSizeConverter.TryGetMb(cacheModel.FilePath, out double mb))
                            {
                                rs.SetResult(false, $"Cache file({model.ProgramPath}: {cacheModel.FilePath}) is not exist!");
                                return false;
                            }
                            model.Mb = mb;
                            if (tk.IsCancellationRequested)
                            {
                                rs.SetResult(false, $"Canceled!: {model.ProgramPath}");
                                return rs;
                            }
                            if (!sftp.IsConnected)
                            {
                                rs.SetResult(false, $"Cannot connect to SFTP server: {model.ProgramPath}");
                                return rs;
                            }
                            try
                            {
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
                    processSignals.Add(sftpWorkerPool.Enqueue(job));
                }
                processSignals.CompleteAdding();
            });
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
