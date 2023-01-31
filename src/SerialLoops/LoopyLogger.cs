using HaruhiChokuretsuLib.Util;

namespace SerialLoops
{
    public class LoopyLogger : ILogger
    {
        public void Log(string message)
        {
            // do nothing for now
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            // do nothing for now
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            // do nothing for now
        }
    }
}
