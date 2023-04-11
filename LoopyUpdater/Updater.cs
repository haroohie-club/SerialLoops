using System.IO.Compression;

namespace LoopyUpdater
{
    public abstract class Updater
    {
        protected const string DOWNLOAD_FILE_NAME = "download";

        public async void Update(string url)
        {
            Console.WriteLine("Downloading update...");
            
            using var client = new HttpClient();
            using var stream = await client.GetStreamAsync(url);
            using var fileStream = new FileStream(DOWNLOAD_FILE_NAME, FileMode.OpenOrCreate);
            await stream.CopyToAsync(fileStream);

            Console.WriteLine("Download complete!");
            
            CopyFiles();
        }

        protected abstract void CopyFiles();

    }

    public class WindowsUpdater : Updater
    {

        protected override void CopyFiles()
        {
            //ZipFile.ExtractToDirectory(DOWNLOAD_FILE_NAME, "download-extracted");
        }
    }
}
