using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Upload.Services.Sftp
{
    public interface ISftpClient : IDisposable
    {
        bool IsConnected { get; }
        bool Connect();
        Task<bool> CreateDirectory(string prodcutPath);
        Task<bool> DeleteFile(string remotePath);
        Task<bool> DeleteFolder(string prodcutPath);
        void Disconnect();
        Task<bool> DownloadFileAsync(string remotePath, string storePath);
        Task<Stream> DownloadFileToStreamAsync(string remotePath);
        Task<bool> Exists(string remotePath);
        Task<List<string>> ListDirectorieNames(string remotePath);
        Task<bool> UploadFileAsync(string remotePath, string localPath);
        Task<bool> UploadStreamFileAsync(Stream zipStream, string remotePath);
    }
}