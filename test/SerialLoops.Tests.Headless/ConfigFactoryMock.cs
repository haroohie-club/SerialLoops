using System;
using System.IO;
using System.Text.Json;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Factories;

namespace SerialLoops.Tests.Headless;

public class ConfigFactoryMock(string configPath, Config config = null) : IConfigFactory
{
    public Config LoadConfig(Func<string, string> localize, ILogger log)
    {
        if (config is not null)
        {
            config.ConfigPath = configPath;
            config.CurrentCultureName = "en-US";
            config.InitializeHacks(log);
            config.Save(log);
            return config;
        }
        else if (File.Exists(configPath))
        {
            Config newConfig = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
            newConfig.ConfigPath = configPath;
            newConfig.InitializeHacks(log);
            return newConfig;
        }
        else
        {
            Config newConfig = ConfigFactory.GetDefault(log);
            newConfig.RememberProjectWorkspace = false;
            newConfig.ConfigPath = configPath;
            newConfig.CurrentCultureName = "en-US"; // there's a chance that the locale will be something we don't recognize (like Invariant culture) so we force this for tests
            newConfig.InitializeHacks(log);
            newConfig.Save(log);
            return newConfig;
        }
    }
}
