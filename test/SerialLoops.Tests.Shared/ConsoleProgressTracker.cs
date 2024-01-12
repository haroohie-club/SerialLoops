using SerialLoops.Lib.Util;

namespace SerialLoops.Tests.Shared
{
    public class ConsoleProgressTracker : IProgressTracker
    {
        public int Finished { get; set; }
        public int Total { get; set; }
        public string CurrentlyLoading { get; set; }
    }
}
