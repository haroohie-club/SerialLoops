using System;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Factories;

public interface IConfigFactory
{
    public ConfigUser LoadConfig(Func<string, string> localize, ILogger log);
}