using System;
using System.IO;
using System.Text.Json;

namespace SerialLoops
{
    public class Config
    {
        public string ProjectsDirectory { get; set; }

        public static Config LoadConfig()
        {
            string configJson = Path.Combine(Environment.CurrentDirectory, "config.json");
            if (!File.Exists(configJson))
            {
                Config defaultConfig = GetDefault();
                File.WriteAllText(configJson, JsonSerializer.Serialize(defaultConfig));
                return defaultConfig;
            }
            else
            {
                try
                {
                    return JsonSerializer.Deserialize<Config>(File.ReadAllText(configJson));
                }
                catch (JsonException exc)
                {
                    // TODO: LOG EXCEPTION
                    Config defaultConfig = GetDefault();
                    File.WriteAllText(configJson, JsonSerializer.Serialize(defaultConfig));
                    return defaultConfig;
                }
            }
        }

        private static Config GetDefault()
        {
            return new Config
            {
                ProjectsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
            };
        }
    }
}
