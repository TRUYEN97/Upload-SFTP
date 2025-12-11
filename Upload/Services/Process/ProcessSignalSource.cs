using System;
using System.Threading.Tasks;
using Upload.Services.Process.Interface;

namespace Upload.Services.Process
{
    public class ProcessSignalSource : IProcessSignalSource
    {
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<T> WaitAsync<T>()
        {
            return (T) await _tcs.Task;
        }

        public Exception Exception { get; private set; }

        public bool SetResult(object result)
        {
            IsRunning = false;
            return _tcs.TrySetResult(result);
        }
        public bool SetException(Exception exception)
        {
            IsRunning = false;
            Exception = exception;
            return _tcs.TrySetResult(default);
        }

        public ProcessSignal Token => new ProcessSignal(this);

        public bool IsRunning { get; private set; } = true;

        public void Reset()
        {
            _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            IsRunning = true;
        }
    }
}
