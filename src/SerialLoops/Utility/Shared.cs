using System.Threading;

namespace SerialLoops.Utility
{
    public static class Shared
    {
        public static CancellationTokenSource AudioReplacementCancellation { get; set; } = new();
    }
}
