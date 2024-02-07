using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SerialLoops.Tests.Shared
{
    public static class TestAssetsDownloader
    {
        public async static Task<string> DownloadTestAssets(string directory = "")
        {
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }
            string zipPath = Path.Combine(directory, "test-assets.zip");
            string directoryPath = Path.Combine(directory, "test-assets");

            using HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, "https://haroohie.blob.core.windows.net/serial-loops/test-assets.zip?sp=r&st=2024-01-15T09:57:56Z&se=2030-01-15T17:57:56Z&spr=https&sv=2022-11-02&sr=b&sig=VoV%2FDWu%2BKF9WpDU57QyF0s2dw6%2FTdEE0OEyLFrbo238%3D");
            HttpResponseMessage response = (await client.SendAsync(request)).EnsureSuccessStatusCode();
            File.WriteAllBytes(zipPath, await response.Content.ReadAsByteArrayAsync());

            using FileStream zipFile = File.OpenRead(zipPath);
            ZipArchive zip = new(zipFile);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
            zip.ExtractToDirectory(directoryPath);

            return directoryPath;
        }

        public static byte[] GetAssetBytes(string assetDir, string assetName)
        {
            return File.ReadAllBytes(Path.Combine(assetDir, assetName));
        }

        public static string GetAssetBase64(string assetDir, string assetName)
        {
            return Convert.ToBase64String(File.ReadAllBytes(Path.Combine(assetDir, assetName)));
        }
    }
}
