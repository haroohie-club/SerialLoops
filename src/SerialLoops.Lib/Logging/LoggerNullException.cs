using System;

namespace SerialLoops.Lib.Logging
{
    public class LoggerNullException : Exception
    {
        public LoggerNullException() : base("No logger provided")
        {
        }
    }
}
