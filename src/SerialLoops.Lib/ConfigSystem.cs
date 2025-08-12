using System.Text.Json.Serialization;

namespace SerialLoops.Lib;

public class ConfigSystem
{
    public string LlvmPath { get; set; }
    public string NinjaPath { get; set; }
    [JsonIgnore]
    public string BundledEmulator { get; set; }
    [JsonIgnore]
    public bool UseUpdater { get; set; }
}
