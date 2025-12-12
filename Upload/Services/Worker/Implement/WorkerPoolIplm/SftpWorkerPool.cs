using System;
using Upload.Config;
using Upload.Services.Sftp;
using Upload.Services.Worker.Implement.JobQeue;
using Upload.Services.Worker.Implement.WorkerIplm;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker.Implement.WorkerPoolIplm
{
    public sealed class SftpWorkerPool : BaseWorkerPool<ISftpClient>
    {
        private static readonly Lazy<SftpWorkerPool> _instance = new Lazy<SftpWorkerPool>(() => new SftpWorkerPool(
            AutoDLConfig.ConfigModel.SftpConfig.MinWorker,
            AutoDLConfig.ConfigModel.SftpConfig.MaxWorker,
            AutoDLConfig.ConfigModel.SftpConfig.QueueCapacity));
        public static SftpWorkerPool Instance => _instance.Value;

        private SftpWorkerPool(int minWorkers, int maxWorkers, int boundedCapacity) : base(minWorkers, maxWorkers, new JobBlockingQueue<ISftpClient>(boundedCapacity)) { }

        protected override IWorker CreateNewWorker(IJobQueue<ISftpClient> queue)
        {
            return new SftpWorker(queue);
        }
    }
}
