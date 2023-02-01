using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Logging
{
    public class LoggerNullException : Exception
    {
        public LoggerNullException() : base("No logger provided")
        {
        }
    }
}
