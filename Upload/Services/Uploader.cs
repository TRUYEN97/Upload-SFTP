using AutoDownload.Gui;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Common;
using Upload.gui;
using Upload.Model;
using Upload.ModelView;
using Upload.Services.Process;
using Upload.Services.Worker.Implement.JobIplm;
using Upload.Services.Worker.Implement.WorkerPoolIplm;
using static Upload.Services.LockManager;

namespace Upload.Services
{
    internal partial class Uploader
    {
        private readonly MyTreeFolderForApp _treeVersion;
        private readonly FormMain _formMain;
        private readonly AccessUserControl _accessControl;
        private AppShowerModel showerModel;
        private readonly string zipPassword;
        private readonly CheckConditon checkConditon;
        private readonly FileProcessSevice fileProcess;
        internal Uploader(FormMain formMain, LocationManagement locationManagement, AccessUserControl accessControl)
        {
            _formMain = formMain;
            zipPassword = ConstKey.ZIP_PASSWORD;
            _treeVersion = new MyTreeFolderForApp(formMain.TreeVersion, zipPassword);
            checkConditon = new CheckConditon(formMain);
            _accessControl = accessControl;
            fileProcess = FileProcessSevice.Instance;
            locationManagement.ShowVerionAction += (v) =>
            {
                ResetData();
                if (v == null)
                {
                    return;
                }
                ShowAppModel(v);
            };
            _treeVersion.OnChosseLanchFile += _formMain.OnChosseLanchFile;
            _treeVersion.OnChosseIconFile += _formMain.OnChosseIconFile;
            _formMain.BtUpdate.Click += (s, e) => _ = UpLoad(locationManagement);
            InitCheckCondition();
        }

        private void InitCheckCondition()
        {
            checkConditon.AddElems("BOM version", _formMain.TxtBOMVersion);
            checkConditon.AddElems("FCD version", _formMain.TxtFCDVersion);
            checkConditon.AddElems("FTU version", _formMain.TxtFTUVersion);
            checkConditon.AddElems("FW version", _formMain.TxtFWVersion);
            checkConditon.AddElems("Launch file", _formMain.TxtLaunchFile);
            checkConditon.AddElems("Icon file", _formMain.TxtIconFile);
            checkConditon.AddElems("Version", _formMain.TxtVersion);
        }

        private async Task UpLoad(LocationManagement locationManagement)
        {
            try
            {
                SetLockFor(true, Reasons.LOCK_UPDATE);
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                if (_accessControl.HasChanged)
                {
                    _accessControl.UpdateData();
                }
                if (await UpdateAppModel())
                {
                    try
                    {
                        ////////////////////////////////
                        var appModel = showerModel.AppModel;
                        if (await fileProcess.UploadFilesAsync(appModel.FileModels.Where(i => i is StoreFileModel).Select(i => i as StoreFileModel).ToList(), zipPassword))
                        {
                            appModel.FileModels = Util.FilterFileModelClass(appModel.FileModels);
                            //////////////////////////////
                            List<FileModel> canDeletes = null;
                            bool success = await SftpWorkerPool.Instance.Enqueue(new SftpJob()
                            {
                                Execute = async (sftp) =>
                                {
                                    if (await ModelUtil.UploadModel(sftp, appModel, appModel.Path, zipPassword))
                                    {
                                        var appList = await ModelUtil.GetModelConfig<AppList>(sftp, appModel.RemoteAppListPath, zipPassword);
                                        canDeletes = await ModelUtil.GetCanDeleteFileModelsAsync(sftp, showerModel.RemoveFileModel, appList, zipPassword);
                                        await locationManagement.UpdateProgramListItems(locationManagement?.Location?.AppName);
                                        return true;
                                    }
                                    return false;
                                }
                            }).WaitAsync<bool>();
                            if (success)
                            {
                                await fileProcess.DeleteFilesAsync(canDeletes);
                                LoggerBox.Addlog("Update done");
                                LoggerBox.Addlog("Update done");
                                return;
                            }
                        }
                        LoggerBox.Addlog("Update failded!!");
                        MessageBox.Show("Update failded!!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error:{ex.Message}");
                        LoggerBox.Addlog($"Update failded:{ex.Message}");
                    }
                }
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
                SetLockFor(false, Reasons.LOCK_UPDATE);
            }
        }
        private CancellationTokenSource _cts;
        private void ShowAppModel(ProgramPathModel programDataPath)
        {
            if (programDataPath == null)
            {
                ResetData();
                return;
            }
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _accessControl.LoadFormPath(programDataPath.AccectUserPath);
            _ = Show(programDataPath.AppPath, _cts);
        }

        private async Task Show(string programDataPath, CancellationTokenSource cts)
        {
            var appModel = await SftpWorkerPool.Instance.Enqueue(new SftpJob()
            {
                Execute = async (sftp) => await ModelUtil.GetModelConfig<AppModel>(sftp, programDataPath, zipPassword)
            }).WaitAsync<AppModel>();
            if (appModel == null)
            {
                ResetProgramData();
                SetLockFor(true, Reasons.LOCK_INPUT);
            }
            else
            {
                if (cts.Token.IsCancellationRequested)
                {
                    return;
                }
                LockManager.Instance.SetLock(false, Reasons.LOCK_INPUT);
                showerModel = new AppShowerModel(appModel);
                _formMain.SetData(appModel);
                if (appModel?.FileModels != null)
                {
                    _treeVersion.StartPopulate(appModel.FileModels, appModel.RemoteStoreDir, _cts);
                }
            }
        }

        private void ResetProgramData()
        {
            _formMain.ClearData();
            _treeVersion.Clear();
        }
        private void ResetData()
        {
            ResetProgramData();
            _accessControl.Clear();
        }

        private async Task<bool> UpdateAppModel()
        {
            if (showerModel == null)
            {
                return false;
            }
            var appModel = showerModel.AppModel;
            var path = appModel.RemoteStoreDir;
            if (appModel == null || path == null || !checkConditon.IsOk())
            {
                return false;
            }
            appModel.LaunchFile = _formMain.TxtLaunchFile.Text?.Trim();
            appModel.IconFile = _formMain.TxtIconFile.Text?.Trim();
            appModel.LastTimeUpdate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            appModel.Version = _formMain.TxtVersion.Text?.Trim();
            appModel.BOMVersion = _formMain.TxtBOMVersion.Text?.Trim();
            appModel.FCDVersion = _formMain.TxtFCDVersion.Text?.Trim();
            appModel.FTUVersion = _formMain.TxtFTUVersion.Text?.Trim();
            appModel.FWSersion = _formMain.TxtFWVersion.Text?.Trim();
            appModel.Enable = _formMain.CbEnabled.Checked;
            appModel.AutoOpen = _formMain.CbAutoOpen.Checked;
            appModel.AutoRemove = _formMain.CbAutoRemove.Checked;
            appModel.AutoUpdate = _formMain.CbAutoUpdate.Checked;
            appModel.CloseAndClear = _formMain.CbCloseAndClear.Checked;
            showerModel.RemoveFileModel.Clear();
            //////////////
            appModel.FileModels = await _treeVersion.GetAllLeafNodes();
            var (hasIcon, hasLaunch) = IsAppFileSelectionOk(appModel);
            if (!hasLaunch)
            {
                LoggerBox.Addlog($"Launch file({appModel.LaunchFile}) not exist!");
                MessageBox.Show($"Launch file({appModel.LaunchFile}) not exist!");
                return false;
            }
            if (!hasIcon)
            {
                LoggerBox.Addlog($"Icon file({appModel.IconFile}) not exist!");
                MessageBox.Show($"Icon file({appModel.IconFile}) not exist!");
                return false;
            }
            showerModel.RemoveFileModel.AddRange(_treeVersion.RemoveFileModel);
            return true;
        }

        private static (bool, bool) IsAppFileSelectionOk(AppModel appModel)
        {
            bool hasIcon = false, hasLaunch = false;
            foreach (var file in appModel.FileModels)
            {
                if (file.ProgramPath == appModel.LaunchFile)
                {
                    hasLaunch = true;
                }
                if (file.ProgramPath == appModel.IconFile)
                {
                    hasIcon = true;
                }
                if (hasIcon && hasLaunch)
                {
                    return (hasIcon, hasLaunch);
                }
            }
            return (hasIcon, hasLaunch);
        }

        private class CheckConditon
        {
            private readonly Dictionary<string, TextBox> textBoxs = new Dictionary<string, TextBox>();
            private readonly Form _own;
            public CheckConditon(Form own)
            {
                _own = own;
            }
            public void AddElems(string name, TextBox textBox)
            {
                if (!textBoxs.ContainsKey(name))
                {
                    textBoxs.Add(name, textBox);
                }
            }

            public void RemoveElems(string name)
            {
                textBoxs.Remove(name);
            }

            public bool IsOk()
            {
                bool ok = true;
                foreach (var textBoxElem in textBoxs)
                {
                    if (string.IsNullOrEmpty(textBoxElem.Value.Text))
                    {
                        Util.SafeInvoke(_own, () => MessageBox.Show(_own, $"{textBoxElem.Key} is empty!"));
                        ok = false;
                    }
                }
                return ok;
            }
        }
    }
}
