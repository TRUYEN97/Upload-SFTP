using System;
using Upload.Services.Sftp;
using Upload.Services.Worker.Implement.JobQeue;
using Upload.Services.Worker.Implement.WorkerIplm;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker.Implement.WorkerPoolIplm
{
    public sealed class SftpWorkerPool : BaseWorkerPool<ISftpClient>
    {
        private static readonly Lazy<SftpWorkerPool> _instance = new Lazy<SftpWorkerPool>(() => new SftpWorkerPool(0, 4));
        public static SftpWorkerPool Instance => _instance.Value;

        private SftpWorkerPool(int minWorkers, int maxWorkers) : base(minWorkers, maxWorkers, new JobBlockingQueue<ISftpClient>(20)) { }

        protected override IWorker CreateNewWorker(IJobQueue<ISftpClient> queue)
        {
            return new SftpWorker(queue);
        }
    }
}
