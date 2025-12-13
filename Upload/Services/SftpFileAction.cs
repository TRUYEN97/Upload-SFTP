
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Common;
using Upload.Model;
using Upload.Services.Process;

namespace Upload.Services
{
    internal static class SftpFileAction
    {
        internal static async Task Open(FileModel fileModel, string localDir)
        {
            try
            {
                string fullPath = Path.GetFullPath(localDir);
                var storeF = await DownloadFileModel(fileModel, fullPath);
                if (storeF != null)
                {
                    Util.OpenFile(storeF.StorePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở file: " + ex.Message);
            }
        }
        internal static async Task<string> Download(FileModel fileModel, string localDir)
        {
            return (await DownloadFileModel(fileModel, localDir))?.StorePath;
        }

        internal static async Task<StoreFileModel> DownloadFileModel(FileModel fileModel, string localDir)
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
                if (await FileProcessSevice.DownloadFileAsync(fileModel, localDir, ConstKey.ZIP_PASSWORD))
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
    }
}
