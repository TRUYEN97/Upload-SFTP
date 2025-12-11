using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace Upload.Common
{
    public static class ProcessUtil
    {
        public static string RunCmd(string command, bool isWaitForExit = true)
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
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (isWaitForExit)
                {
                    process.WaitForExit();
                }
                return string.IsNullOrEmpty(error) ? output : error;
            }
        }

        /// <summary>
        /// Tìm process đang chạy file cụ thể (.exe, .jar, .py, .bat, ...)
        /// </summary>
        /// <param name="filePath">Đường dẫn đầy đủ của file (vd: C:\app\test.jar)</param>
        /// <returns>Process nếu tìm thấy, ngược lại trả về null</returns>
        public static Process FindProcessByFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            string[] possibleProcessNames = null;
            switch (extension)
            {
                case ".exe":
                    possibleProcessNames = new[] { Path.GetFileNameWithoutExtension(filePath) };
                    break;
                case ".jar":
                    possibleProcessNames = new[] { "java", "javaw" };
                    break;
                case ".py":
                    possibleProcessNames = new[] { "python", "pythonw" };
                    break;
                case ".bat":
                    possibleProcessNames = new[] { "cmd" };
                    break;
            }
            foreach (string procName in possibleProcessNames)
            {
                foreach (var process in Process.GetProcessesByName(procName))
                {
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher(
                            $"SELECT ExecutablePath, CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                        {
                            var result = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                            if (result == null) continue;

                            string exePath = result["ExecutablePath"]?.ToString();
                            string cmdLine = result["CommandLine"]?.ToString();
                            bool match = false;
                            if (extension == ".exe")
                            {
                                if (!string.IsNullOrEmpty(exePath) &&
                                    string.Equals(exePath, filePath, StringComparison.OrdinalIgnoreCase))
                                    match = true;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(cmdLine) &&
                                    cmdLine.IndexOf(filePath, StringComparison.OrdinalIgnoreCase) >= 0)
                                    match = true;
                            }
                            if (match)
                                return process;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        public static Process RunProcess(string process, string arguments = null, string workingDirectory = null, bool useShellExecute = false, bool createNoWindow = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = process,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory ?? Path.GetFullPath(Path.GetDirectoryName(process)),
                UseShellExecute = useShellExecute,
                CreateNoWindow = createNoWindow
            };
            return Process.Start(psi);
        }
    }
}
