
namespace Upload.Config
{
    public class SftpConfig
    {
        public int MinWorker { get; set; } = 0;
        public int MaxWorker { get; set; } = 10;
        public int QueueCapacity { get; set; } = 100;
        public string Host {  get; set; }
        public int Port {  get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
