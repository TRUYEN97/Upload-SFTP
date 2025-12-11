using System;
using System.Threading.Tasks;
using Upload.Services.Process.Interface;

namespace Upload.Services.Process
{
    public class ProcessSignal : IProcessSignal
    {
        private readonly ProcessSignalSource _source;
        public ProcessSignal(ProcessSignalSource source)
        {
            _source = source;
        }

        public Task<T> WaitAsync<T>() => _source.WaitAsync<T>();

        public Exception Exception => _source.Exception;

        public bool IsRunning => _source.IsRunning;
    }
}