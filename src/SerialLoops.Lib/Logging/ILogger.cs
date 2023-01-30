namespace SerialLoops.Lib.Logging
{
    public interface ILogger
    {
        public void Log(string message);
        public void LogError(string message, bool lookForWarnings = false);
        public void LogWarning(string message, bool lookForErrors = false);
    }
}
