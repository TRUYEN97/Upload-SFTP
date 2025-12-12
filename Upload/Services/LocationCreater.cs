using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Common;
using Upload.Model;
using Upload.Services.Process;
using Upload.Services.Worker.Implement.JobIplm;
using Upload.Services.Worker.Implement.WorkerPoolIplm;
using WinSCP;

namespace Upload.Services
{
    internal class LocationCreater
    {

        private readonly string zipPassword;
        private readonly SftpWorkerPool sftpWorkerPool;
        internal LocationCreater(string zipPassword)
        {
            this.zipPassword = zipPassword;
            sftpWorkerPool = SftpWorkerPool.Instance;
        }

        internal async Task<bool> CreateProduct(Location location)
        {
            if (string.IsNullOrWhiteSpace(location.Product))
            {
                return false;
            }
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                return await sftpWorkerPool.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
                        string prodcutPath = PathUtil.GetProductPath(location);
                        if (await sftp.Exists(prodcutPath))
                        {
                            Util.ShowMessager($"Product {location.Product} has exists!");
                            return true;
                        }
                        if (await sftp.CreateDirectory(prodcutPath))
                        {
                            Util.ShowCreatedMessager("Product", location.Product);
                            return true;
                        }
                        Util.ShowCreateFailedMessager("Product", location.Product);
                        return false;
                    }
                }).WaitAsync<bool>();
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }
        internal async Task<bool> DeleteProduct(Location location)
        {
            if (string.IsNullOrWhiteSpace(location.Product))
            {
                return false;
            }
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                return await sftpWorkerPool.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
                        string prodcutPath = PathUtil.GetProductPath(location);
                        if (!await sftp.Exists(prodcutPath))
                        {
                            Util.ShowNotFoundMessager("Product", location.Product);
                            return true;
                        }
                        if (await sftp.DeleteFolder(prodcutPath))
                        {
                            Util.ShowCreatedMessager("Product", location.Product);
                            return true;
                        }
                        Util.ShowCreateFailedMessager("Product", location.Product);
                        return false;
                    }
                }).WaitAsync<bool>();
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }
        internal async Task<bool> CreateStation(Location location)
        {
            if (string.IsNullOrWhiteSpace(location.Product) || string.IsNullOrWhiteSpace(location.Station))
            {
                return false;
            }
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                ThreadPool.GetAvailableThreads(out int wk, out int completionPortThreads);
                return await sftpWorkerPool.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
                        string path = PathUtil.GetStationPath(location);
                        if (await sftp.Exists(path))
                        {
                            Util.ShowNotFoundMessager("Station", location.Station);
                            return true;
                        }
                        string commonFilePath = PathUtil.GetCommonPath(location);
                        if (!await sftp.Exists(commonFilePath) && !await sftp.CreateDirectory(commonFilePath))
                        {
                            Util.ShowCreateFailedMessager($"{location.Station}/Common", "");
                            return false;
                        }
                        if (!await sftp.CreateDirectory(path))
                        {
                            Util.ShowCreateFailedMessager("station", location.Station);
                            return false;
                        }
                        await ModelUtil.UploadModel(sftp, new AccessUserListModel(), PathUtil.GetStationAccessUserPath(location), zipPassword);
                        Util.ShowCreatedMessager("Station", location.Station);
                        return true;
                    }
                }).WaitAsync<bool>();

            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }

        }

        internal async Task<bool> DeleteStation(Location location)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(location.Product) || string.IsNullOrWhiteSpace(location.Station))
                {
                    return false;
                }
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                return await sftpWorkerPool.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
                        Location loca = new Location(location);
                        string path = PathUtil.GetStationPath(loca);
                        if (!await sftp.Exists(path))
                        {
                            Util.ShowDeletedMessager("Station", loca.Station);
                            return true;
                        }
                        var (appList, _) = await ModelUtil.GetAppListModel(sftp, loca, zipPassword);
                        if (appList?.ProgramPaths == null || appList?.ProgramPaths.Count == 0)
                        {
                            if (await sftp.DeleteFolder(path))
                            {
                                Util.ShowDeletedMessager("Station", loca.Station);
                                return true;
                            }
                        }
                        Util.ShowDeleteFailedMessager("Station", loca.Station);
                        return false;
                    }
                }).WaitAsync<bool>();
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }

        }

        internal async Task<bool> CreateProgram(Location location)
        {
            if (string.IsNullOrWhiteSpace(location.AppName))
            {
                return false;
            }
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                if (await CreateAppModel(location))
                {
                    Util.ShowCreatedMessager("Program", location.AppName);
                    return true;
                }
                Util.ShowCreateFailedMessager("Program", location.AppName);
                return false;
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }

        }
        private async Task<bool> CreateAppModel(Location location)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                return await sftpWorkerPool.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
                        Location loca = new Location(location);
                        if (string.IsNullOrWhiteSpace(loca.Product)
                            || string.IsNullOrWhiteSpace(loca.Station)
                            || string.IsNullOrWhiteSpace(loca.AppName))
                        {
                            return false;
                        }
                        var (model, remoteAppListPath) = await ModelUtil.GetAppListModel(sftp, loca, zipPassword);
                        if (model == null)
                        {
                            model = new AppList();
                        }
                        if (model.ProgramPaths.ContainsKey(loca.AppName))
                        {
                            return true;
                        }
                        string programDataPath = PathUtil.GetAppModelPath(loca);
                        if (!await ModelUtil.UploadModel(sftp, new AppModel()
                        {
                            RemoteStoreDir = PathUtil.GetCommonPath(loca),
                            RemoteAppListPath = remoteAppListPath,
                            Path = programDataPath,
                            Version = $"{DateTime.Now:\\Vyyyy\\.MM\\.dd}"
                        }, programDataPath, zipPassword))
                        {
                            Util.ShowMessager($"Station [{loca.Station}] create [{loca.AppName}] app data failed!");
                            return false;
                        }
                        string programAccessUserPath = PathUtil.GetAppAccessUserPath(loca);
                        if (!await ModelUtil.UploadModel(sftp, new AccessUserListModel(), programAccessUserPath, zipPassword))
                        {
                            Util.ShowMessager($"Station [{loca.Station}] create access user for [{loca.AppName}] failed!");
                            return false;
                        }
                        model.ProgramPaths.Add(loca.AppName, new ProgramPathModel()
                        {
                            AccectUserPath = programAccessUserPath,
                            AppPath = programDataPath
                        });
                        return (await ModelUtil.UpLoadAppListModel(sftp, model, loca, zipPassword)).Item1;
                    }
                }).WaitAsync<bool>();
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }
        internal async Task<bool> DuplicateProgram(Location location, string newName)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                return await sftpWorkerPool.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
                        Location loca = new Location(location);
                        if (string.IsNullOrWhiteSpace(loca.Product)
                            || string.IsNullOrWhiteSpace(loca.Station)
                            || string.IsNullOrWhiteSpace(newName)
                            || string.IsNullOrWhiteSpace(loca.AppName))
                        {
                            return false;
                        }
                        var (model, remoteAppListPath) = await ModelUtil.GetAppListModel(sftp, loca, zipPassword);
                        if (model == null)
                        {
                            model = new AppList();
                        }
                        if (model.ProgramPaths.ContainsKey(newName))
                        {
                            Util.ShowMessager($"Program [{newName}] has exists!");
                            return false;
                        }
                        if (!model.ProgramPaths.TryGetValue(loca.AppName, out var sourcePathModel))
                        {
                            Util.ShowMessager("Get soruce path model failed!");
                            return false;
                        }
                        ////////////// copy model //////////////////////////////
                        ///
                        var sourceAppModel = await ModelUtil.GetModelConfig<AppModel>(sftp, sourcePathModel.AppPath, zipPassword);
                        if (sourceAppModel == null)
                        {
                            Util.ShowMessager($"Get soruce App model failed!");
                            return false;
                        }
                        var newLocation = new Location(loca) { AppName = newName };
                        string programDataPath = PathUtil.GetAppModelPath(newLocation);
                        sourceAppModel.Path = programDataPath;
                        sourceAppModel.RemoteStoreDir = PathUtil.GetCommonPath(newLocation);
                        sourceAppModel.RemoteAppListPath = remoteAppListPath;
                        sourceAppModel.Enable = false;
                        sourceAppModel.Version = $"{DateTime.Now:\\Vyyyy\\.MM\\.dd}";
                        if (!await ModelUtil.UploadModel(sftp, sourceAppModel, programDataPath, zipPassword))
                        {
                            Util.ShowMessager($"Station [{newLocation.Station}] create [{newName}] app data failed!");
                            return false;
                        }
                        //////////////////////////////////////////////////
                        ///
                        ///////// copy appAccessUserList //////////
                        var appAccessUserList = await ModelUtil.GetModelConfig<AccessUserListModel>(sftp, sourcePathModel.AccectUserPath, zipPassword);
                        string programAccessUserPath = PathUtil.GetAppAccessUserPath(newLocation);
                        if (!await ModelUtil.UploadModel(sftp, appAccessUserList, programAccessUserPath, zipPassword))
                        {
                            Util.ShowMessager($"Station [{newLocation.Station}] create access user for [{newName}] failed!");
                            return false;
                        }
                        ////////////////////////////////////////
                        model.ProgramPaths.Add(newLocation.AppName, new ProgramPathModel()
                        {
                            AccectUserPath = programAccessUserPath,
                            AppPath = programDataPath
                        });
                        return (await ModelUtil.UpLoadAppListModel(sftp, model, newLocation, zipPassword)).Item1;
                    }
                }).WaitAsync<bool>();
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }
        internal async Task<bool> DeleteProgram(Location location)
        {
            if (string.IsNullOrWhiteSpace(location.Product) || string.IsNullOrWhiteSpace(location.Station) || string.IsNullOrWhiteSpace(location.AppName))
            {
                return false;
            }
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                Location loca = new Location(location);
                List<FileModel> canDeletes = null;
                bool seccuess = await sftpWorkerPool.Enqueue(new SftpJob()
                {
                    Execute = async (sftp) =>
                    {
                        var (appList, remoteAppListPath) = await ModelUtil.GetAppListModel(sftp, loca, zipPassword);
                        if (appList?.ProgramPaths != null)
                        {
                            if (appList.ProgramPaths.TryGetValue(loca.AppName, out var modelPath))
                            {
                                AppModel appModel = await ModelUtil.GetModelConfig<AppModel>(sftp, modelPath.AppPath, zipPassword);
                                if (appModel?.FileModels != null)
                                {
                                    canDeletes = await ModelUtil.GetCanDeleteFileModelsAsync(sftp, appModel.FileModels.ToList(), appList, zipPassword, modelPath.AppPath);
                                    await sftp.DeleteFile(modelPath.AppPath);
                                }
                                await sftp.DeleteFile(modelPath.AccectUserPath);
                                appList.ProgramPaths.Remove(loca.AppName);
                                if (!await ModelUtil.UploadModel(sftp, appList, remoteAppListPath, zipPassword))
                                {
                                    Util.ShowDeleteFailedMessager(location.AppName, "");
                                    return false;
                                }
                            }
                        }
                        Util.ShowDeletedMessager(location.AppName, "");
                        return true;
                    }
                }).WaitAsync<bool>();
                if (seccuess)
                {
                    await FileProcessSevice.Instance.DeleteFilesAsync(canDeletes);
                    return true;
                }
                return false;
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }
    }
}
