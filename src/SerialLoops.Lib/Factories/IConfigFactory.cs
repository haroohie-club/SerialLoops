using System;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Factories
{
    public interface IConfigFactory
    {
        public Config LoadConfig(Func<string, string> localize, ILogger log);
    }
}
