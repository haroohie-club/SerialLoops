using System;
using System.IO;
using System.Text.Json;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Factories;

namespace SerialLoops.Tests.Shared
{
    public class ConfigFactoryMock(string configPath) : IConfigFactory
    {
        private string _configPath = configPath;

        public Config LoadConfig(Func<string, string> localize, ILogger log)
        {
            if (File.Exists(_configPath))
            {
                Config newConfig = JsonSerializer.Deserialize<Config>(File.ReadAllText(_configPath));
                newConfig.ConfigPath = _configPath;
                return newConfig;
            }
            else
            {
                Config newConfig = ConfigFactory.GetDefault(log);
                newConfig.ConfigPath = _configPath;
                newConfig.CurrentCultureName = "en-US"; // there's a chance that the locale will be something we don't recognize (like Invariant culture) so we force this for tests
                newConfig.Save(log);
                return newConfig;
            }
        }
    }
}
