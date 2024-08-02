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
                newConfig.Save(log);
                return newConfig;
            }
        }
    }
}
