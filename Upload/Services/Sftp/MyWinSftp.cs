namespace Upload.Services.Sftp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Upload.Services;
    using WinSCP;

    public class MyWinSftp : IDisposable, ISftpClient
    {
        private readonly SessionOptions _options;
        private Session _session;

        public Action<string> Logger;
        public Action<TransferProgress> Progress;

        public int MaxRetry { get; set; } = 3;
        public bool IsConnected
        {
            get
            {
                try
                {
                    return _session?.Opened == true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public MyWinSftp(string host, int port, string user, string pass)
        {
            _options = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = host,
                PortNumber = port,
                UserName = user,
                Password = pass,
                SshHostKeyPolicy = SshHostKeyPolicy.GiveUpSecurityAndAcceptAny
            };
            Connect();
        }

        private void Log(string msg)
        {
            Logger?.Invoke($"[{DateTime.Now:HH:mm:ss}] {msg}");
        }
        public bool Connect()
        {
            if (IsConnected)
                return true;
            _session = new Session();
            _session.FileTransferProgress += (sender, e) =>
            {
                Progress?.Invoke(new TransferProgress
                {
                    FilePercent = e.FileProgress * 100,
                    OverallPercent = e.OverallProgress * 100
                });
            };
            Stopwatch stopwatch = new Stopwatch(15000);
            Exception exception = null;
            while (!IsConnected && stopwatch.IsOntime)
            {
                try
                {
                    _session.Open(_options);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }
            if (exception != null)
            {
                throw exception;
            }
            return IsConnected;
        }

        public void Disconnect()
        {
            try
            {
                if (_session?.Opened == true)
                {
                    _session.Dispose();
                    _session = null;
                }
            }
            catch
            {
            }
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> action)
        {
            int attempt = 0;
            Exception lastEx = null;
            while (attempt < MaxRetry)
            {
                try
                {
                    attempt++;
                    if (!IsConnected)
                    {
                        return default;
                    }
                    return await action();
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    Log($"Retry {attempt}/{MaxRetry} failed → {ex.Message}");
                    Connect();
                }
            }

            throw lastEx;
        }

        public async Task<bool> Exists(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return false;
            }
            remotePath = remotePath.Replace("\\", "/");
            return await RetryAsync(async () => await Task.Run(() => _session.FileExists(remotePath)));
        }
        public async Task<bool> CreateDirectory(string remoteFolder)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        CreateDir(remoteFolder);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            });
        }

        public async Task<bool> DeleteFile(string remote)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        remote = remote.Replace("\\", "/");
                        _session.RemoveFiles(remote).Check();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            });
        }

        public async Task<bool> DeleteFolder(string remoteFolder)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        remoteFolder = remoteFolder.Replace("\\", "/");
                        _session.RemoveFiles(remoteFolder + "/*").Check();
                        _session.RemoveFiles(remoteFolder).Check();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            });
        }

        public async Task<List<string>> ListFiles(string remoteFolder)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    var list = new List<string>();
                    remoteFolder = remoteFolder.Replace("\\", "/");
                    foreach (var f in _session.ListDirectory(remoteFolder).Files)
                    {
                        if (!f.IsDirectory && !f.Name.StartsWith("."))
                            list.Add(f.FullName);
                    }
                    return list;
                });
            });
        }

        public async Task<List<string>> ListDirectories(string remoteFolder)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    var list = new List<string>();
                    remoteFolder = remoteFolder.Replace("\\", "/");
                    foreach (var f in _session.ListDirectory(remoteFolder).Files)
                    {
                        if (f.IsDirectory && !f.Name.StartsWith("."))
                            list.Add(f.FullName);
                    }
                    return list;
                });
            });
        }
        public async Task<List<string>> ListFileNames(string remoteFolder)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    var list = new List<string>();
                    remoteFolder = remoteFolder.Replace("\\", "/");
                    foreach (var f in _session.ListDirectory(remoteFolder).Files)
                    {
                        if (!f.IsDirectory && !f.Name.StartsWith("."))
                            list.Add(f.Name);
                    }
                    return list;
                });
            });
        }

        public async Task<List<string>> ListDirectorieNames(string remoteFolder)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    remoteFolder = remoteFolder.Replace("\\", "/");
                    var list = new List<string>();
                    foreach (var f in _session.ListDirectory(remoteFolder).Files)
                    {
                        if (f.IsDirectory && !f.Name.StartsWith("."))
                            list.Add(f.Name);
                    }
                    return list;
                });
            });
        }
        public async Task<bool> UploadFileAsync(string remotePath, string localPath)
        {
            if (string.IsNullOrWhiteSpace(remotePath) || string.IsNullOrWhiteSpace(localPath))
            {
                return false;
            }
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        remotePath = remotePath.Replace("\\", "/");
                        localPath = localPath.Replace("/", "\\");
                        var opts = new TransferOptions
                        {
                            TransferMode = TransferMode.Binary
                        };
                        CreateDir(Path.GetDirectoryName(remotePath));
                        Log($"Uploading: {localPath} → {remotePath}");
                        var result = _session.PutFiles(localPath, remotePath, false, opts);
                        result.Check();
                        Log("Upload OK");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log($"Upload error: {ex.Message}");
                        return false;
                    }
                });
            });
        }
        public async Task<bool> UploadStreamFileAsync(Stream stream, string remotePath)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(async () =>
                {
                    try
                    {
                        if (stream.CanSeek)
                            stream.Position = 0;
                        await Task.Yield();
                        var opts = new TransferOptions
                        {
                            TransferMode = TransferMode.Binary,
                            OverwriteMode = OverwriteMode.Overwrite
                        };
                        remotePath = remotePath.Replace("\\", "/");
                        CreateDir(Path.GetDirectoryName(remotePath));
                        Log($"Uploading stream -> {remotePath}");
                        _session.PutFile(stream, remotePath, opts);
                        Log("Upload OK");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log($"Upload error: {ex.Message}");
                        return false;
                    }
                });
            });
        }

        public async Task<Stream> DownloadFileToStreamAsync(string remotePath)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        var opts = new TransferOptions
                        {
                            TransferMode = TransferMode.Binary,
                            OverwriteMode = OverwriteMode.Overwrite
                        };
                        remotePath = remotePath.Replace("\\", "/");
                        Log($"Download {remotePath} -> stream");
                        if (!_session.FileExists(remotePath))
                        {
                            return null;
                        }
                        var stream = _session.GetFile(remotePath, opts);
                        if (stream?.CanSeek == true)
                            stream.Position = 0;
                        return stream;
                    }
                    catch (Exception ex)
                    {
                        Log($"Upload error: {ex.Message}");
                        return null;
                    }
                });
            });
        }

        public async Task<bool> DownloadFileAsync(string remotePath, string localPath)
        {
            return await RetryAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    var opts = new TransferOptions
                    {
                        TransferMode = TransferMode.Binary,
                        OverwriteMode = OverwriteMode.Overwrite
                    };
                    remotePath = remotePath.Replace("\\", "/");
                    localPath = localPath.Replace("/", "\\");
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                    var result = _session.GetFiles(remotePath, localPath, false, opts);
                    result.Check();
                    return true;
                });
            });
        }

        private void CreateDir(string remoteFolder)
        {
            if (string.IsNullOrWhiteSpace(remoteFolder))
            {
                return;
            }
            List<string> parents = new List<string>() { remoteFolder.Replace("\\", "/") };
            var parentInfo = Path.GetDirectoryName(remoteFolder).Replace("\\", "/");
            while (!_session.FileExists(parentInfo))
            {
                parents.Add(parentInfo);
                parentInfo = Path.GetDirectoryName(parentInfo).Replace("\\", "/");
            }
            parents.Reverse();
            foreach (var parent in parents)
            {
                if (!_session.FileExists(parent))
                {
                    _session.CreateDirectory(parent);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                Disconnect();
            }
            catch
            {
            }
        }
    }

}
