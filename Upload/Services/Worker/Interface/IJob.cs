using System;
using System.Threading;
using System.Threading.Tasks;

namespace Upload.Services.Worker.Interface
{
    public interface IJob<T>
    {
        CancellationToken CancellationToken { get; }
        Func<T, Task<object>> Execute { get; }
        Guid Id { get; }
    }
}
