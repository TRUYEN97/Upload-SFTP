using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Upload.Services.Process.Interface;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker.Implement.WorkerPoolIplm
{
    public abstract class BaseWorkerPool<T> : IWorkerPool<T>
    {
        protected readonly IJobQueue<T> _queue;
        private readonly List<(IWorker worker, CancellationTokenSource cts)> _workers;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly int _maxWorkers;
        private readonly int _minWorkers;
        private readonly System.Timers.Timer timer;
        private static readonly object _lockWoker = new object();
        protected BaseWorkerPool(int minWorkers, int maxWorkers, IJobQueue<T> queue)
        {
            ThreadPool.GetMinThreads(out int wk, out int completionPortThreads);
            ThreadPool.SetMinThreads(maxWorkers + wk, completionPortThreads);
            _queue = queue ?? throw new ArgumentNullException("queue == null!");
            _workers = new List<(IWorker, CancellationTokenSource)>();
            _minWorkers = minWorkers < 0 ? 0 : minWorkers;
            _maxWorkers = maxWorkers < minWorkers ? minWorkers : maxWorkers;
            timer = new System.Timers.Timer() { AutoReset = true, Interval = 1000 };
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                try
                {
                    int queueCount = _queue.Count;
                    int workerCount = _workers.Count;
                    if (queueCount > workerCount * 5 || (queueCount > 0 && workerCount == 0))
                    {
                        AddWorker();
                    }
                    else
                    if (queueCount < workerCount * 2 && (workerCount > 1 || queueCount == 0))
                    {
                        RemoveWorker();
                    }
                }
                finally
                {
                    timer.Start();
                }
            };
            timer.Start();
        }
        protected abstract IWorker CreateNewWorker(IJobQueue<T> queue);

        public IProcessSignal Enqueue(IJob<T> sftpJob)
        {
            if (_workers.Count == 0)
            {
                AddWorker();
            }
            return _queue.Enqueue(sftpJob);
        }

        private void AddWorker()
        {
            lock (_lockWoker)
            {
                if (_workers.Count >= _maxWorkers)
                    return;
                var cancelToken = new CancellationTokenSource();
                var worker = CreateNewWorker(_queue);
                if (worker == null) return;
                Task.Run(async () => await worker.WorkerLoop(cancelToken.Token));
                _workers.Add((worker, cancelToken));
            }
        }

        private void RemoveWorker()
        {
            lock (_lockWoker)
            {
                int count = _workers.Count;
                if (count >= _minWorkers && count > 0)
                {
                    try
                    {
                        var (worker, cts) = _workers[count - 1];
                        cts?.Cancel();
                        worker?.Dispose();
                    }
                    catch { }
                    _workers.RemoveAt(count - 1);
                }
            }
        }

        public void Dispose()
        {
            timer.Dispose();
            _queue.Dispose();
            _cts.Cancel();
            foreach (var worker in _workers)
            {
                worker.cts?.Cancel();
                worker.worker?.Dispose();
            }
            _workers.Clear();
        }
    }
}
