using HaroohieClub.NitroPacker.Core;
using SerialLoops.Lib.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SerialLoops.Lib
{
    public static class IO
    {
        public static void OpenRom(Project project, string romPath)
        {
            NdsProjectFile.Create(project.Name, romPath, project.BaseDirectory);
            NdsProjectFile.Create(project.Name, romPath, project.IterativeDirectory);
        }

        public static async Task FetchAssets(Project project, Uri assetsRepoZip, Uri stringsRepoZip, ILogger log)
        {
            using HttpClient client = new();
            string assetsZipPath = Path.Combine(project.MainDirectory, "assets.zip");
            string stringsZipPath = Path.Combine(project.MainDirectory, "strings.zip");
            try
            {
                File.WriteAllBytes(assetsZipPath, await client.GetByteArrayAsync(assetsRepoZip));
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred during assets zip fetch.\n{exc.Message}\n\n{exc.StackTrace}");
            }
            try
            {
                File.WriteAllBytes(stringsZipPath, await client.GetByteArrayAsync(stringsRepoZip));
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred during strings zip fetch.\n{exc.Message}\n\n{exc.StackTrace}");
            }

            string assetsBasePath = Path.Combine(project.BaseDirectory, "assets");
            string assetsIterativePath = Path.Combine(project.IterativeDirectory, "assets");
            string stringsBasePath = Path.Combine(project.BaseDirectory, "strings");
            string stringsIterativePath = Path.Combine(project.IterativeDirectory, "strings");
            try
            {
                ZipFile.ExtractToDirectory(assetsZipPath, assetsBasePath);
                ZipFile.ExtractToDirectory(assetsZipPath, assetsIterativePath);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred during unzipping assets zip.\n{exc.Message}\n\n{exc.StackTrace}");
            }
            try
            {
                ZipFile.ExtractToDirectory(stringsZipPath, stringsBasePath);
                ZipFile.ExtractToDirectory(stringsZipPath, stringsIterativePath);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred during unzipping strings zip.\n{exc.Message}\n\n{exc.StackTrace}");
            }

            try
            {
                File.Delete(assetsZipPath);
                File.Delete(stringsZipPath);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred during deleting zip files.\n{exc.Message}\n\n{exc.StackTrace}");
            }
        }

        public static void CopyFiles(string sourceDirectory, string destinationDirectory)
        {
            foreach (string file in Directory.GetFiles(sourceDirectory,"*", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(destinationDirectory, file));
            }
        }

        public static bool WriteStringFile(string file, string str, ILogger log)
        {
            try
            {
                File.WriteAllText(file, str);
                return true;
            }
            catch (IOException exc)
            {
                log.LogError($"Exception occurred while writing file '{file}' to disk.\n{exc.Message}\n\n{exc.StackTrace}");
                return false;
            }
        }
    }
}
