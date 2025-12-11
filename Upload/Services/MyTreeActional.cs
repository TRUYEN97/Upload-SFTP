using AutoDownload.Gui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Common;
using Upload.Config;
using Upload.gui;
using Upload.Model;
using Upload.Services.Cache;
using Upload.Services.Process;
using static Upload.Services.LockManager;

namespace Upload.Services
{
    internal class MyTreeActional
    {
        private static readonly Color FILE_UPDATE_COLOR = Color.Yellow;
        private static readonly Color FILE_RENAME_COLOR = Color.Orange;
        private static readonly Color FILE_CREATE_COLOR = Color.Green;
        private static readonly Color FOLDER_UPDATE_COLOR = Color.YellowGreen;
        private static readonly Color FOLDER_RENAME_COLOR = Color.Gold;
        private static readonly Color FOLDER_CREATE_COLOR = Color.DarkBlue;
        private static readonly Color FILE_COLOR = Color.AntiqueWhite;
        private static readonly Color FOLDER_COLOR = Color.Black;
        private static readonly Color TREE_BACK_COLOR = Color.Gray;
        private static readonly Color TREE_LINE_COLOR = Color.Yellow;
        private static readonly Font FILE_FONT = new Font("Microsoft Sans Serif", 9.0F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
        private static readonly Font FOLDER_FONT = new Font("Microsoft Sans Serif", 10.0F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
        private static readonly string FOLDER_KEY = "folder";

        private readonly FileProcessSevice fileProcessSevice;
        private readonly TreeView _treeView;
        private readonly ImageList _imageList;

        public HashSet<FileModel> RemoveFileModel { get; }

        public string RemoteDir { get; set; }
        public MyTreeActional(TreeView treeView)
        {
            fileProcessSevice = FileProcessSevice.Instance;
            _treeView = treeView;
            _treeView.BackColor = TREE_BACK_COLOR;
            _treeView.LineColor = TREE_LINE_COLOR;
            _imageList = new ImageList();
            RemoveFileModel = new HashSet<FileModel>();
            _treeView.ImageList = _imageList;
            if (!_imageList.Images.ContainsKey(FOLDER_KEY))
            {
                _imageList.Images.Add(FOLDER_KEY, SystemIcons.Information);
            }
        }
        internal async Task UpdateFolderToNodeIterative(TreeNodeCollection rootNodes, string rootFolderPath)
        {
            try
            {
                using (ProgressDialogForm form = new ProgressDialogForm($"Add folder: {rootFolderPath}"))
                {
                    await form.DoworkAsync(async (report, token) =>
                    {
                        await Task.Run(() =>
                        {
                            int count = 0;
                            CursorUtil.SetCursorIs(Cursors.WaitCursor);
                            ConfirmOverrideForm confirmOverrideFile = new ConfirmOverrideForm();
                            var stack = new Stack<(TreeNodeCollection nodes, string path)>();
                            foreach (string file in Directory.GetFiles(rootFolderPath))
                            {
                                report?.Invoke(++count, file);
                                AddNewMode(rootNodes, file, confirmOverrideFile, true);
                                if (token.IsCancellationRequested)
                                    return;
                            }
                            foreach (string subfolder in Directory.GetDirectories(rootFolderPath))
                            {
                                stack.Push((rootNodes, subfolder));
                                if (token.IsCancellationRequested)
                                    return;
                            }
                            while (stack.Count > 0)
                            {
                                if (token.IsCancellationRequested)
                                    return;
                                var (currentNodes, currentPath) = stack.Pop();
                                string folderName = Path.GetFileName(currentPath);
                                TreeNode folderNode = new TreeNode(folderName);
                                folderNode = AddNewFolderNode(currentNodes, folderNode, true);
                                TreeNodeCollection childNodes = folderNode.Nodes;
                                foreach (string file in Directory.GetFiles(currentPath))
                                {
                                    report?.Invoke(++count, file);
                                    AddNewMode(childNodes, file, confirmOverrideFile, true);
                                    if (token.IsCancellationRequested)
                                        return;
                                }
                                foreach (string subfolder in Directory.GetDirectories(currentPath))
                                {
                                    stack.Push((childNodes, subfolder));
                                    if (token.IsCancellationRequested)
                                        return;
                                }
                            }
                        });
                    });
                }
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }


        internal async Task AddFolderToNodeIterative(TreeNodeCollection rootNodes, string rootFolderPath)
        {
            try
            {
                using (ProgressDialogForm form = new ProgressDialogForm($"Add folder: {rootFolderPath}"))
                {
                    await form.DoworkAsync(async (report, token) =>
                    {
                        await Task.Run(() =>
                        {
                            int count = 0;
                            CursorUtil.SetCursorIs(Cursors.WaitCursor);
                            ConfirmOverrideForm confirmOverrideFile = new ConfirmOverrideForm();
                            var stack = new Stack<(TreeNodeCollection nodes, string path)>();
                            stack.Push((rootNodes, rootFolderPath));
                            bool checkUnique = FindFolder(rootNodes, Path.GetFileName(rootFolderPath)) != null;
                            while (stack.Count > 0)
                            {
                                if (token.IsCancellationRequested)
                                    return;
                                var (currentNodes, currentPath) = stack.Pop();
                                string folderName = Path.GetFileName(currentPath);
                                TreeNode folderNode = new TreeNode(folderName);
                                folderNode = AddNewFolderNode(currentNodes, folderNode, checkUnique);
                                TreeNodeCollection childNodes = folderNode.Nodes;
                                foreach (string file in Directory.GetFiles(currentPath))
                                {
                                    report?.Invoke(++count, file);
                                    AddNewMode(childNodes, file, confirmOverrideFile, checkUnique);
                                    if (token.IsCancellationRequested)
                                        return;
                                }
                                foreach (string subfolder in Directory.GetDirectories(currentPath))
                                {
                                    stack.Push((childNodes, subfolder));
                                    if (token.IsCancellationRequested)
                                        return;
                                }
                            }
                        });
                    });
                }
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        internal async Task Download(string folderPath, HashSet<FileModel> fileModels)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                Util.ShowMessager($"Download to folder: {folderPath}");
                if (await fileProcessSevice.DownloadFilesAsync(fileModels, folderPath, ConstKey.ZIP_PASSWORD))
                {
                    Util.ShowMessager($"Download to folder: {folderPath} OK");
                    MessageBox.Show($"Download to folder: {folderPath} OK");
                }
                else
                {
                    Util.ShowMessager($"Download to folder: {folderPath} failed");
                    MessageBox.Show($"Download to folder: {folderPath} failed");
                }
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        internal async Task<HashSet<FileModel>> GetFileModels(TreeNode selectedNode)
        {
            if (selectedNode?.Nodes?.Count > 0)
            {
                return await GetAllLeafNodes(selectedNode.Nodes);
            }
            else
            {
                var list = new HashSet<FileModel>();
                if (selectedNode?.Tag is FileModel fileModel)
                {
                    list.Add(fileModel);
                }
                return list;
            }
        }
        internal async Task AddFilesToNodeIterative(TreeNodeCollection nodes, string[] fileNames)
        {
            try
            {
                using (ProgressDialogForm form = new ProgressDialogForm("Add Files"))
                {
                    await form.DoworkAsync(async (report, token) =>
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                LockManager.Instance.SetLock(true, Reasons.LOCK_UPDATE);
                                ConfirmOverrideForm confirmOverrideFile = new ConfirmOverrideForm();
                                foreach (string filePath in fileNames)
                                {
                                    AddNewMode(nodes, filePath, confirmOverrideFile);
                                }
                            }
                            finally
                            {
                                LockManager.Instance.SetLock(false, Reasons.LOCK_UPDATE);
                            }
                        });
                    });
                }
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }


        internal TreeNode AddFolder(TreeNodeCollection nodes, TreeNode folderNode)
        {
            folderNode.ForeColor = FOLDER_COLOR;
            return AddFolderNode(nodes, folderNode, false);
        }

        private TreeNode AddFolderNode(TreeNodeCollection nodes, TreeNode folderNode, bool checkUnique = true)
        {
            folderNode.ImageKey = FOLDER_KEY;
            folderNode.SelectedImageKey = FOLDER_KEY;
            TreeNode oldNode = checkUnique ? FindFolder(nodes, folderNode.Text) : null;
            Util.SafeInvoke(_treeView, () =>
            {
                folderNode.NodeFont = FOLDER_FONT;
                if (oldNode == null)
                {
                    nodes.Add(folderNode);
                }
                else
                {
                    folderNode = oldNode;
                    folderNode.ForeColor = FOLDER_UPDATE_COLOR;
                }
            });
            return folderNode;
        }

        private void AddNewMode(TreeNodeCollection nodes, string filePath, ConfirmOverrideForm confirmOverrideFile = null, bool checkUnique = true)
        {
            TreeNode node = CreateNewNode(filePath);
            AddFileNode(nodes, node, confirmOverrideFile, checkUnique);
        }

        private TreeNode CreateNewNode(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            TreeNode newNode = new TreeNode(fileName)
            {
                Tag = new StoreFileModel()
                {
                    StorePath = filePath,
                    RemoteDir = RemoteDir
                }
            };
            return newNode;
        }

        internal void AddFileNode(TreeNodeCollection nodes, TreeNode fileNode, ConfirmOverrideForm confirmOverrideFile = null, bool checkUnique = true)
        {
            string fileName = fileNode.Text;
            string ext = Path.GetExtension(fileName).ToLower();
            if (string.IsNullOrEmpty(ext))
            {
                ext = "__no_ext__";
            }
            if (!_imageList.Images.ContainsKey(ext))
            {
                Util.SafeInvoke(_treeView, () =>
                {
                    try
                    {
                        Icon icon = Util.GetIconForExtension(fileName);
                        _imageList.Images.Add(ext, icon);
                    }
                    catch
                    {
                        _imageList.Images.Add(ext, SystemIcons.Application.ToBitmap());
                    }
                });
            }
            fileNode.ImageKey = ext;
            fileNode.SelectedImageKey = ext;
            TreeNode oldNode = checkUnique ? FindFile(nodes, fileNode.Text) : null;
            Util.SafeInvoke(_treeView, () =>
            {
                fileNode.NodeFont = FILE_FONT;
                if (oldNode == null)
                {
                    nodes.Add(fileNode);
                    if (fileNode.Tag is StoreFileModel storeFileModel)
                    {
                        fileNode.ForeColor = FILE_CREATE_COLOR;
                        string fullPath = fileNode.Parent?.FullPath;
                        if (string.IsNullOrWhiteSpace(fullPath))
                        {
                            storeFileModel.ProgramPath = fileName;
                        }
                        else
                        {
                            storeFileModel.ProgramPath = Path.Combine(fullPath, fileName);
                        }
                    }
                    else
                    {
                        fileNode.ForeColor = FILE_COLOR;
                    }
                }
                else if (confirmOverrideFile == null || confirmOverrideFile.IsAccept($"[{fileName}] has exists!\r\nDo you want to update this file?"))
                {
                    UpdateFileModel(nodes, fileNode, oldNode);
                }
            });

        }
        internal void UpdateFile(TreeNode fileNodeOld, string newFilePath)
        {
            TreeNodeCollection treeNodeCollection = fileNodeOld?.Parent?.Nodes ?? _treeView.Nodes;
            TreeNode newNode = CreateNewNode(newFilePath);
            UpdateFileModel(treeNodeCollection, newNode, fileNodeOld);
        }

        private TreeNode UpdateFileModel(TreeNodeCollection nodes, TreeNode newNode, TreeNode oldNode)
        {
            newNode.ForeColor = FILE_UPDATE_COLOR;
            newNode.Text = oldNode.Text;
            if (oldNode.Tag is FileModel fileModel)
            {
                if (!(oldNode.Tag is StoreFileModel))
                {
                    RemoveFileModel.Add(fileModel);
                }
                if (newNode.Tag is StoreFileModel storeFileModel)
                {
                    storeFileModel.ProgramPath = fileModel.ProgramPath;
                }
            }
            Util.SafeInvoke(_treeView, () =>
            {
                oldNode.Remove();
                nodes.Add(newNode);
            });
            return newNode;
        }



        internal TreeNode AddNewFolderNode(TreeNodeCollection nodes, TreeNode newNode, bool checkUnique)
        {
            newNode.ForeColor = FOLDER_CREATE_COLOR;
            return AddFolderNode(nodes, newNode, checkUnique);
        }

        internal async Task OpenFile(TreeNode treeNode)
        {
            if (treeNode?.Tag is FileModel fileModel)
            {
                if (fileModel is StoreFileModel storeFileModel && File.Exists(storeFileModel.StorePath))
                {
                    Util.OpenFile(storeFileModel.StorePath);
                }
                else
                {
                    await SftpFileAction.Open(fileModel, AutoDLConfig.ConfigModel.TempDir);
                }
            }
        }

        internal async Task EditFile(TreeNode treeNode)
        {
            if (treeNode?.Tag is FileModel fileModel)
            {
                if (fileModel is StoreFileModel storeFileModel && File.Exists(storeFileModel.StorePath))
                {
                    Util.RunCmd($"NotePad \"{storeFileModel.StorePath}\"");
                }
                else
                {
                    string storePath = await SftpFileAction.Download(fileModel, AutoDLConfig.ConfigModel.TempDir);
                    Util.RunCmd($"NotePad \"{storePath}\"", false);
                    UpdateFile(treeNode, storePath);
                }
            }
        }

        internal async Task Delete(TreeNode selectedNode)
        {
            if (selectedNode == null)
            {
                return;
            }
            using (ProgressDialogForm form = new ProgressDialogForm("Delete Files"))
            {
                HashSet<FileModel> nodes = await GetFileModels(selectedNode);
                if (nodes.Count > 0)
                {
                    form.Maximum = nodes.Count;
                    HashSet<FileModel> toRemove = await form.DoworkAsync(async (report, token) => await GetRemoveFileModels(report, nodes, token));
                    if (toRemove?.Count > 0)
                    {
                        foreach (var FileModelItem in toRemove)
                        {
                            RemoveFileModel.Add(FileModelItem);
                        }
                    }
                }
                Util.SafeInvoke(_treeView, () => selectedNode.Remove());
                Util.ShowMessager($"Delete folder [{selectedNode.Text}]");
            }
        }

        internal async Task ClearAll()
        {
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (TreeNode node in _treeView.Nodes)
            {
                nodes.Add(node);
            }
            foreach (TreeNode node in nodes)
            {
                await Delete(node);
            }
        }

        private static async Task<HashSet<FileModel>> GetRemoveFileModels(Action<int, string> report, HashSet<FileModel> nodes, CancellationToken token)
        {
            return await Task.Run(() =>
            {
                int count = 0;
                HashSet<FileModel> rm = new HashSet<FileModel>();
                foreach (FileModel filemodel in nodes)
                {
                    if (!(filemodel is StoreFileModel))
                    {
                        report(++count, filemodel.ProgramPath);
                        rm.Add(filemodel);
                        Util.ShowMessager($"delete [{filemodel.ProgramPath}]");
                    }
                    if (token.IsCancellationRequested)
                    {
                        Util.ShowMessager("Cancel!");
                        return null;
                    }
                }
                return rm;
            });
        }

        internal void RenameNode(TreeNode rootNode, string newName)
        {
            Task.Run(() =>
            {
                try
                {
                    CursorUtil.SetCursorIs(Cursors.WaitCursor);

                    var parent = rootNode.Parent;
                    if (parent != null)
                    {
                        var oldNode = FindFolder(parent.Nodes, newName) ?? FindFile(parent.Nodes, newName);
                        if (oldNode != null)
                        {
                            LoggerBox.Addlog($"Name:[{newName}] matches the path name is: [{oldNode.FullPath}]");
                        }
                    }

                    Util.ShowMessager($"Rename: [{rootNode.Text}] -> [{newName}]");
                    Util.SafeInvoke(_treeView, () =>
                    {
                        rootNode.Text = newName;
                        if (rootNode.Tag is FileModel)
                        {
                            rootNode.ForeColor = FILE_RENAME_COLOR;
                        }
                        else
                        {
                            rootNode.ForeColor = FOLDER_RENAME_COLOR;
                        }
                    });
                    var stack = new Stack<TreeNode>();
                    stack.Push(rootNode);

                    while (stack.Count > 0)
                    {
                        TreeNode current = stack.Pop();
                        if (current.Tag is FileModel model)
                        {
                            model.ProgramPath = current.FullPath;
                            current.ForeColor = FILE_RENAME_COLOR;
                        }
                        else
                        {
                            current.ForeColor = FOLDER_RENAME_COLOR;
                            for (int i = current.Nodes.Count - 1; i >= 0; i--)
                            {
                                stack.Push(current.Nodes[i]);
                            }
                        }
                    }
                    Util.ShowMessager($"Rename: [{rootNode.Text}] -> [{newName}] done");
                }
                finally
                {
                    CursorUtil.SetCursorIs(Cursors.Default);
                }
            });
        }

        internal async Task<HashSet<FileModel>> GetAllLeafNodes(TreeNodeCollection nodes)
        {
            try
            {
                CursorUtil.SetCursorIs(Cursors.WaitCursor);
                HashSet<FileModel> leafNodes = new HashSet<FileModel>();
                if (nodes == null) return leafNodes;
                Stack<TreeNode> stack = new Stack<TreeNode>();
                return await Task.Run(() =>
                {
                    foreach (TreeNode node in nodes)
                    {
                        stack.Push(node);
                    }

                    while (stack.Count > 0)
                    {
                        TreeNode currentNode = stack.Pop();

                        if (currentNode.Nodes.Count == 0 && currentNode.Tag is FileModel fileModel)
                        {
                            leafNodes.Add(fileModel);
                        }
                        else
                        {
                            foreach (TreeNode child in currentNode.Nodes)
                            {
                                stack.Push(child);
                            }
                        }
                    }
                    return leafNodes;
                });
            }
            finally
            {
                CursorUtil.SetCursorIs(Cursors.Default);
            }
        }

        internal TreeNode FindFolder(TreeNodeCollection nodes, string text)
        {
            TreeNode oldNode = null;
            foreach (TreeNode node in nodes)
            {
                if (node.Text.Equals(text, StringComparison.OrdinalIgnoreCase) && !(node.Tag is FileModel) && node.ImageKey == FOLDER_KEY)
                {
                    oldNode = node;
                    break;
                }
            }
            return oldNode;
        }
        internal TreeNode FindFile(TreeNodeCollection nodes, string text)
        {
            TreeNode oldNode = null;
            foreach (TreeNode node in nodes)
            {
                if (node.Text.Equals(text, StringComparison.OrdinalIgnoreCase) && node.Tag is FileModel)
                {
                    oldNode = node;
                    break;
                }
            }
            return oldNode;
        }

        internal TreeNode SelectedNode => _treeView.SelectedNode;

        internal TreeNodeCollection GetNodeCollection()
        {
            TreeNodeCollection nodes;
            if (_treeView.SelectedNode != null)
            {
                if (_treeView.SelectedNode.Tag is FileModel)
                {
                    return null;
                }
                nodes = _treeView.SelectedNode.Nodes;
            }
            else
            {
                nodes = _treeView.Nodes;
            }
            return nodes;
        }

        internal string[] GetParts(FileModel fileModel)
        {
            if (fileModel == null || fileModel.ProgramPath == null)
            {
                return new string[] { };
            }
            string normalizedPath = fileModel.ProgramPath.Replace('/', Path.DirectorySeparatorChar)
                                    .Replace('\\', Path.DirectorySeparatorChar);
            return normalizedPath.Split(Path.DirectorySeparatorChar);
        }
    }

}
