using System;
using System.Threading;
using System.Threading.Tasks;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker.Implement.WorkerIplm
{
    public abstract class BaseWorker<T> : IWorker
    {
        private readonly IJobQueue<T> _jobQueue;
        protected readonly CancellationTokenSource _cts;
        protected BaseWorker(IJobQueue<T> jobQueue)
        {
            _jobQueue = jobQueue;
            _cts = new CancellationTokenSource();
        }
        protected abstract T GetParamater();
        protected abstract void OnAfterShift();
        public async Task WorkerLoop(CancellationToken ctsToken)
        {
            try
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }
                foreach (var model in await _jobQueue.GetConsumingEnumerable(ctsToken))
                {
                    var job = model.Job;
                    var result = model.SignalSource;
                    try
                    {
                        result.SetResult(await job.Execute(GetParamater()));
                    }
                    catch (Exception ex)
                    {
                        result.SetException(ex);
                    }
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                OnAfterShift();
            }
        }

        public virtual void Dispose()
        {
            _cts.Cancel();
        }
    }
}
