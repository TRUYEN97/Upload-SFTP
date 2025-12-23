using Upload.Common;
using Upload.Services.Sftp;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker.Implement.WorkerIplm
{
    public class SftpWorker : BaseWorker<ISftpClient>
    {
        private ISftpClient _client;
        private readonly object _lockObject = new object();
        public SftpWorker(IJobQueue<ISftpClient> jobQueue) : base(jobQueue) { }

        protected override ISftpClient GetParamater()
        {
            if (_client?.IsConnected != true)
            {
                lock (_lockObject)
                {
                    if (_client?.IsConnected != true)
                    {
                        _client = Util.GetSftpInstance();
                        _client.Connect();
                        return _client;
                    }
                }
            }
            return _client;
        }

        protected override void OnAfterShift()
        {
            try
            {
                _client?.Disconnect();
            }
            catch
            {
            }
            try
            {
                _client?.Dispose();
            }
            catch
            {
            }
        }
    }
}
