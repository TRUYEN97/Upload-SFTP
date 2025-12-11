using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Upload.Services.Process;
using Upload.Services.Process.Interface;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker.Implement.JobQeue
{
    public class JobBlockingQueue<T> : IJobQueue<T>
    {

        private readonly BlockingCollection<WorkerModel<T>> _queue;
        public JobBlockingQueue(int boundedCapacity = 0)
        {
            _queue = boundedCapacity < 1 ?
                new BlockingCollection<WorkerModel<T>>() :
                new BlockingCollection<WorkerModel<T>>(boundedCapacity);
        }

        public int Count => _queue.Count;

        public void Dispose()
        {
            _queue.CompleteAdding();
            _queue.Dispose();
        }

        public IProcessSignal Enqueue(IJob<T> sftpJob)
        {
            if (sftpJob == null)
            {
                throw new ArgumentNullException($"{GetType().Name}: job is null");
            }
            var model = new WorkerModel<T>()
            {
                Job = sftpJob,
                SignalSource = new ProcessSignalSource()
            };
            _queue.Add(model);
            return model.SignalSource.Token;
        }

        public async Task<IEnumerable<WorkerModel<T>>> GetConsumingEnumerable(CancellationToken token)
        {
            return await Task.Run(() => _queue.GetConsumingEnumerable(token));
        }
    }
}
