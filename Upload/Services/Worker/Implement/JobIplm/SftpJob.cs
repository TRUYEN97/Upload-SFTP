using System;
using System.Threading;
using System.Threading.Tasks;
using Upload.Services.Sftp;
using Upload.Services.Worker.Interface;

namespace Upload.Services.Worker.Implement.JobIplm
{
    public class SftpJob : IJob<ISftpClient>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Func<ISftpClient, Task<object>> Execute { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
