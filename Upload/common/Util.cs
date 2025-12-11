using AutoDownload.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Upload.Services.Sftp;
using Upload.Config;
using Upload.Model;

namespace Upload.Common
{
    public static class Util
    {

        internal static void SetCursor(Form form, Cursor cursor)
        {
            Util.SafeInvoke(form, () => { form.Cursor = cursor; });
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi,
        uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        internal static HashSet<FileModel> FilterFileModelClass(ICollection<FileModel> fileModels)
        {
            HashSet<FileModel> rs = new HashSet<FileModel>();
            if (fileModels != null)
            {
                foreach (var fileModel in fileModels)
                {
                    if (fileModel is StoreFileModel storeFileModel)
                    {
                        rs.Add(storeFileModel.FileModel());
                    }
                    else
                    {
                        rs.Add(fileModel);
                    }
                }
            }
            return rs;
        }

        const uint SHGFI_ICON = 0x000000100;
        const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        internal static Icon GetIconForExtension(string fileNameOrExt)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            SHGetFileInfo(fileNameOrExt, FILE_ATTRIBUTE_NORMAL, ref shinfo,
                (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_USEFILEATTRIBUTES);

            return Icon.FromHandle(shinfo.hIcon);
        }
        public static void SafeInvoke(Control control, Action updateAction)
        {
            try
            {
                if (control == null)
                {
                    return;
                }
                if (control.InvokeRequired)
                {
                    control.Invoke(updateAction);
                }
                else
                {
                    updateAction();
                }
            }
            catch
            {
            }
        }

        internal static void ShowNotFoundMessager(string type, string stationName)
        {
            ShowMessager($"{type} {stationName} not found!");
        }

        internal static void ShowCreateFailedMessager(string type, string station)
        {
            ShowMessager($"Create {type} {station} failed!");
        }
        internal static void ShowCreatedMessager(string type, string station)
        {
            ShowMessager($"Create {type} {station} ok!");
        }

        internal static void ShowDeleteFailedMessager(string type, string station)
        {
            ShowMessager($"Delete {type} {station} failed!");
        }
        internal static void ShowDeletedMessager(string type, string station)
        {
            ShowMessager($"Delete {type} {station} ok!");
        }
        public static void ShowConnectFailedMessager()
        {
            ShowMessager("Connect to server failed!");
        }
        public static void ShowMessager(string mess)
        {
            LoggerBox.Addlog(mess);
        }
        public static bool DeleteDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    return false;
                }
                bool rs = true;
                Stack<string> dirs = new Stack<string>();
                dirs.Push(path);
                while (dirs.Count > 0)
                {
                    string dir = dirs.Pop();
                    if (Directory.Exists(dir))
                    {
                        foreach (var file in Directory.GetFiles(dir))
                        {
                            if (File.Exists(file))
                            {
                                try
                                {
                                    File.SetAttributes(file, FileAttributes.Normal);
                                    File.Delete(file);
                                }
                                catch (Exception)
                                {
                                    rs = false;
                                }
                            }
                        }
                        foreach (var subDir in Directory.GetDirectories(dir))
                        {
                            dirs.Push(subDir);
                        }
                    }
                }
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                return rs;
            }
            catch (Exception)
            {
                return false;
            }
        }
        internal static void OpenFile(string storePath)
        {
            RunCmd(storePath, false);
        }
        public static void RunCmd(string command, bool isWaitForExit = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C " + command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(psi))
            {
                if (isWaitForExit)
                {
                    process.WaitForExit();
                }
            }
        }

        public static bool ArePathsEqual(string path1, string path2)
        {
            string normalizedPath1 = Path.GetFullPath(path1).TrimEnd(Path.DirectorySeparatorChar);
            string normalizedPath2 = Path.GetFullPath(path2).TrimEnd(Path.DirectorySeparatorChar);

            return string.Equals(normalizedPath1, normalizedPath2, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetMD5HashFromFile(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash)
                        sb.Append(b.ToString("x2"));
                    return sb.ToString();
                }
            }
        }

        internal static MyWinSftp GetSftpInstance()
        {
            var _configModel = AutoDLConfig.ConfigModel;
            return new MyWinSftp(
                _configModel.SftpConfig.Host,
                _configModel.SftpConfig.Port,
                _configModel.SftpConfig.User,
                _configModel.SftpConfig.Password);
        }


        internal static string GetMD5HashFromString(string input)
        {
            if (input == null)
            {
                return null;
            }
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static bool CopyFile(string sourceFile, string tagetFile)
        {
            if (!File.Exists(sourceFile))
            {
                return false;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(tagetFile));
            File.Copy(sourceFile, tagetFile, true);
            return true;
        }

        public static bool Rename(string sourceFile, string tagetFile)
        {
            if (File.Exists(tagetFile) || !File.Exists(sourceFile))
            {
                return false;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(tagetFile));
            File.Move(sourceFile, tagetFile);
            return true;
        }

        public static bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
