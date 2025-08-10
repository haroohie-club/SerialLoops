using System.Text.Json.Serialization;

namespace SerialLoops.Lib;

public class ConfigSystem
{
    public string LlvmPath { get; set; }
    public string NinjaPath { get; set; }
    [JsonIgnore]
    public string FlatpakProcess { get; set; }
    [JsonIgnore]
    public string[] FlatpakProcessBaseArgs { get; set; }
    [JsonIgnore]
    public string FlatpakRunProcess { get; set; }
    [JsonIgnore]
    public string[] FlatpakRunProcessBaseArgs { get; set; }
    [JsonIgnore]
    public bool UseUpdater { get; set; }
}
