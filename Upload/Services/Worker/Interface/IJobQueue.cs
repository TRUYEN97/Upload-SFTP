using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Upload.Services.Process.Interface;

namespace Upload.Services.Worker.Interface
{
    public interface IJobQueue<T>: IDisposable
    {
        int Count { get; }
        IProcessSignal Enqueue(IJob<T> sftpJob);
        Task<IEnumerable<WorkerModel<T>>> GetConsumingEnumerable(CancellationToken token);
    }
}
