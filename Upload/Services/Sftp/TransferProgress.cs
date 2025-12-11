namespace Upload.Services.Sftp
{
    public class TransferProgress
    {
        public double FilePercent { get; set; }     // 0 - 100
        public double OverallPercent { get; set; }  // 0 - 100

        public override string ToString()
            => $"File: {FilePercent:0.00}%, All: {OverallPercent:0.00}%";
    }
}
