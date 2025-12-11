
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Upload.Common;
using Upload.Model;

namespace Upload.Services
{
    internal static class SftpFileAction
    {
        internal static async Task Open(FileModel fileModel, string localDir)
        {
            try
            {
                string fullPath = Path.GetFullPath(localDir);
                var storeF = await ModelUtil.Download(fileModel, fullPath, ConstKey.ZIP_PASSWORD);
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
            string fullPath = Path.GetFullPath(localDir);
            return (await ModelUtil.Download(fileModel, fullPath, ConstKey.ZIP_PASSWORD))?.StorePath;
        }
    }
}
