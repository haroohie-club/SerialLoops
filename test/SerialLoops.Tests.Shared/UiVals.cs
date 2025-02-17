using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SerialLoops.Tests.Shared;

public class UiVals
{
    public string AssetsDirectory { get; set; } = string.Empty;
    [JsonIgnore] public static string? BaseDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
}
