using System;
using System.Threading;
using System.Threading.Tasks;

namespace Upload.Services.Worker.Interface
{
    public interface IWorker : IDisposable
    {
        Task WorkerLoop(CancellationToken token);
    }
}