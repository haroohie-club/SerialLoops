using System;
using System.IO;
using System.Text.Json;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Factories;

namespace SerialLoops.Tests.Headless;

public class ConfigFactoryMock(string configPath, ConfigUser configUser = null) : IConfigFactory
{
    public ConfigUser LoadConfig(Func<string, string> localize, ILogger log)
    {
        if (configUser is not null)
        {
            configUser.ConfigPath = configPath;
            configUser.SysConfig = ConfigFactory.GetDefaultSystem(true, log);
            configUser.CurrentCultureName = "en-US";
            configUser.CheckForUpdates = false;
            configUser.RememberProjectWorkspace = false;
            configUser.InitializeHacks(log);
            configUser.Save(log);
            return configUser;
        }
        else if (File.Exists(configPath))
        {
            ConfigUser newConfigUser = JsonSerializer.Deserialize<ConfigUser>(File.ReadAllText(configPath));
            newConfigUser.SysConfig = ConfigFactory.GetDefaultSystem(true, log);
            newConfigUser.ConfigPath = configPath;
            newConfigUser.InitializeHacks(log);
            return newConfigUser;
        }
        else
        {
            ConfigUser newConfigUser = ConfigFactory.GetDefault(ConfigFactory.GetDefaultSystem(true, log), log);
            newConfigUser.RememberProjectWorkspace = false;
            newConfigUser.ConfigPath = configPath;
            newConfigUser.CurrentCultureName = "en-US"; // there's a chance that the locale will be something we don't recognize (like Invariant culture) so we force this for tests
            newConfigUser.InitializeHacks(log);
            newConfigUser.Save(log);
            return newConfigUser;
        }
    }
}
