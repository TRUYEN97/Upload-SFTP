using System;
using System.Threading.Tasks;

namespace Upload.Services.Process.Interface
{
    public interface IProcessSignal
    {
        bool IsRunning { get; }
        Exception Exception { get; }
        Task<T> WaitAsync<T>();
    }
}