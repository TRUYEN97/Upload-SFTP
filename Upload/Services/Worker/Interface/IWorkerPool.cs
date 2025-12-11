using System;
using Upload.Services.Process.Interface;

namespace Upload.Services.Worker.Interface
{
    public interface IWorkerPool<T>: IDisposable
    {
        IProcessSignal Enqueue(IJob<T> sftpJob);
    }
}