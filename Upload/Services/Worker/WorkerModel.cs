using Upload.Services.Process.Interface;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker
{
    public class WorkerModel<T>
    {
        public IProcessSignalSource SignalSource { get; set; }
        public IJob<T> Job { get; set; }
    }
}
