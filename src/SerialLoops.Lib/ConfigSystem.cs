using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib;

public class ConfigSystem
{
    public string LlvmPath { get; set; }
    public string NinjaPath { get; set; }
    public string EmulatorFlatpak { get; set; }
    public string EmulatorPath { get; set; }
    [JsonIgnore]
    public bool StoreSysConfig { get; set; }
    [JsonIgnore]
    public bool StoreEmulatorPath => string.IsNullOrEmpty(BundledEmulator) && !StoreSysConfig;
    [JsonIgnore]
    public string BundledEmulator { get; set; }
    [JsonIgnore]
    public bool UseUpdater { get; set; }

    public void Save(string sysConfigPath, ILogger log)
    {
        IO.WriteStringFile(sysConfigPath, JsonSerializer.Serialize(this), log);
    }
}

public class ConfigEmulator
{
    public static string EmulatorConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SerialLoops",
        "emulator.json");
    public string EmulatorFlatpak { get; set; }
    public string EmulatorPath { get; set; }

    public void Write()
    {
        File.WriteAllText(EmulatorConfigPath, JsonSerializer.Serialize(this));
    }
}
