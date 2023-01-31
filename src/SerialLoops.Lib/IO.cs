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
            // Unpack the ROM, creating the two project directories
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.BaseDirectory, "rom"));
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.IterativeDirectory, "rom"));

            // Create our structure for building the ROM
            Directory.CreateDirectory(Path.Combine(project.BaseDirectory, "original", "archives"));
            Directory.CreateDirectory(Path.Combine(project.BaseDirectory, "original", "overlay"));
            Directory.CreateDirectory(Path.Combine(project.BaseDirectory, "original", "bgm"));
            Directory.CreateDirectory(Path.Combine(project.BaseDirectory, "original", "vce"));
            Directory.CreateDirectory(Path.Combine(project.IterativeDirectory, "original", "archives"));
            Directory.CreateDirectory(Path.Combine(project.IterativeDirectory, "original", "overlay"));
            Directory.CreateDirectory(Path.Combine(project.IterativeDirectory, "original", "bgm"));
            Directory.CreateDirectory(Path.Combine(project.IterativeDirectory, "original", "vce"));
            Directory.CreateDirectory(Path.Combine(project.BaseDirectory, "src", "source"));
            Directory.CreateDirectory(Path.Combine(project.IterativeDirectory, "src", "source"));
            Directory.CreateDirectory(Path.Combine(project.BaseDirectory, "src", "replSource"));
            Directory.CreateDirectory(Path.Combine(project.IterativeDirectory, "src", "replSource"));
            Directory.CreateDirectory(Path.Combine(project.BaseDirectory, "src", "overlays"));
            Directory.CreateDirectory(Path.Combine(project.IterativeDirectory, "src", "overlays"));

            // Copy out the files we need to build the ROM
            File.Copy(Path.Combine(project.BaseDirectory, "rom", "arm9.bin"), Path.Combine(project.BaseDirectory, "src", "arm9.bin"));
            File.Copy(Path.Combine(project.IterativeDirectory, "rom", "arm9.bin"), Path.Combine(project.IterativeDirectory, "src", "arm9.bin"));
            CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data"), Path.Combine(project.BaseDirectory, "original", "archives"), "*.bin");
            CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data"), Path.Combine(project.IterativeDirectory, "original", "archives"), "*.bin");
            CopyFiles(Path.Combine(project.BaseDirectory, "rom", "overlay"), Path.Combine(project.BaseDirectory, "original", "overlay"));
            CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "overlay"), Path.Combine(project.IterativeDirectory, "original", "overlay"));

            // We conditionalize these so we can test on a non-copyrighted ROM; this should always be true with real data
            if (Directory.Exists(Path.Combine(project.BaseDirectory, "rom", "data", "bgm")))
            {
                CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data", "bgm"), Path.Combine(project.BaseDirectory, "original", "bgm"));
                CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data", "bgm"), Path.Combine(project.IterativeDirectory, "original", "bgm"));
                CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data", "vce"), Path.Combine(project.BaseDirectory, "original", "vce"));
                CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data", "vce"), Path.Combine(project.IterativeDirectory, "original", "vce"));
            }

            // Copy out static files used during build
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x"), Path.Combine(project.BaseDirectory, "src", "linker.x"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x"), Path.Combine(project.IterativeDirectory, "src", "linker.x"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x"), Path.Combine(project.BaseDirectory, "src", "overlays", "linker.x"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x"), Path.Combine(project.IterativeDirectory, "src", "overlays", "linker.x"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_main"), Path.Combine(project.BaseDirectory, "src", "Makefile"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_main"), Path.Combine(project.IterativeDirectory, "src", "Makefile"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_overlay"), Path.Combine(project.BaseDirectory, "src", "overlays", "Makefile"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_overlay"), Path.Combine(project.IterativeDirectory, "src", "overlays", "Makefile"));
        }

        public static void FetchAssets(Project project, Uri assetsRepoZip, Uri stringsRepoZip, ILogger log)
        {
            FetchAssetsAsync(project, assetsRepoZip, stringsRepoZip, log).GetAwaiter().GetResult();
        }

        public static async Task FetchAssetsAsync(Project project, Uri assetsRepoZip, Uri stringsRepoZip, ILogger log)
        {
            using HttpClient client = new();
            string assetsZipPath = Path.Combine(project.MainDirectory, "assets.zip");
            string stringsZipPath = Path.Combine(project.MainDirectory, "strings.zip");
            try
            {
                File.WriteAllBytes(assetsZipPath, await client.GetByteArrayAsync(assetsRepoZip));
            }
            catch (HttpRequestException exc)
            {
                if (exc.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    log.LogWarning($"Failed to download assets zip. Please join the Haroohie Translation Club Discord and request an assets bundle there.");
                }
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred during assets zip fetch.\n{exc.Message}\n\n{exc.StackTrace}");
            }
            try
            {
                File.WriteAllBytes(stringsZipPath, await client.GetByteArrayAsync(stringsRepoZip));
            }
            catch (HttpRequestException exc)
            {
                if (exc.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    log.LogWarning($"Failed to download assets zip. Please join the Haroohie Translation Club Discord and request an assets bundle there.");
                }
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
                CopyFiles(assetsBasePath, assetsIterativePath);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred during unzipping assets zip.\n{exc.Message}\n\n{exc.StackTrace}");
            }
            try
            {
                ZipFile.ExtractToDirectory(stringsZipPath, stringsBasePath);
                CopyFiles(stringsBasePath, stringsIterativePath);
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

        public static void SetUpLocalizedHacks(Project project)
        {
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks", "fontOffset.c"), Path.Combine(project.BaseDirectory, "src", "source", "fontOffset.c"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks", "fontOffset.c"), Path.Combine(project.IterativeDirectory, "src", "source", "fontOffset.c"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks", "fontOffset_asm.s"), Path.Combine(project.BaseDirectory, "src", "source", "fontOffset_asm.s"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks", "fontOffset_asm.s"), Path.Combine(project.IterativeDirectory, "src", "source", "fontOffset_asm.s"));
        }

        public static void CopyFiles(string sourceDirectory, string destinationDirectory, string filter = "*")
        {
            foreach (string file in Directory.GetFiles(sourceDirectory, filter, SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)));
            }
        }

        public static void DeleteFilesKeepDirectories(string sourceDirectory)
        {
            foreach (string directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                foreach (string file in Directory.GetFiles(directory))
                {
                    File.Delete(file);
                }
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
