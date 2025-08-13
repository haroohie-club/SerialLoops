using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialLoops.Tests.Shared;

public class UiVals
{
    public string AssetsDirectory { get; set; } = string.Empty;
    [JsonIgnore] public static string? BaseDirectory => AppContext.BaseDirectory;

    public static async Task<UiVals> DownloadTestAssets()
    {
        HttpClient client = new() { DefaultRequestHeaders = { Accept = { new("*/*") }}};
        await using Stream zipStream = await client.GetStreamAsync(
            "https://haroohie.nyc3.cdn.digitaloceanspaces.com/bootstrap/serial-loops/test-assets.zip");
        ZipArchive archive = new(zipStream);
        UiVals uiVals = new() { AssetsDirectory = Path.Join(BaseDirectory ?? string.Empty, "assets") };
        archive.ExtractToDirectory(uiVals.AssetsDirectory);
        await File.WriteAllTextAsync("ui_vals.json", JsonSerializer.Serialize(uiVals));
        return uiVals;
    }
}
