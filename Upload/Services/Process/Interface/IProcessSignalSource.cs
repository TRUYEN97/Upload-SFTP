using System;
using System.Threading.Tasks;

namespace Upload.Services.Process.Interface
{
    public interface IProcessSignalSource: IProcessSignal
    {
        ProcessSignal Token { get; }
        void Reset();
        bool SetResult(object result);
        bool SetException(Exception exception);
    }
}