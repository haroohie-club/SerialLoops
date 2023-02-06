using HaruhiChokuretsuLib.Util;
using HaroohieClub.NitroPacker.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SerialLoops.Lib
{
    public static class IO
    {
        private class IODirectory
        {
            public string Name { get; set; }
            public IODirectory[] Subdirectories { get; set; }
            public IOFile[] Files { get; set; }

            public IODirectory(string name, IODirectory[] subdirectories, IOFile[] files)
            {
                Name = name;
                Subdirectories = subdirectories;
                Files = files;
            }

            public void Create(string basePath)
            {
                string dirPath = Path.Combine(basePath, Name);
                Directory.CreateDirectory(dirPath);
                foreach (IOFile file in Files)
                {
                    File.Copy(file.FilePath, Path.Combine(dirPath, file.Name));
                }
                foreach (IODirectory subdirectory in Subdirectories)
                {
                    subdirectory.Create(dirPath);
                }
            }
        }

        private class IOFile
        {
            public string FilePath { get; set; }
            public string Name { get; set; }

            public IOFile(string path)
            {
                FilePath = path;
                Name = Path.GetFileName(FilePath);
            }

            public IOFile(string path, string name)
            {
                FilePath = path;
                Name = name;
            }
        }

        public static void OpenRom(Project project, string romPath)
        {
            // Unpack the ROM, creating the two project directories
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.BaseDirectory, "rom"));
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.IterativeDirectory, "rom"));

            // Create our structure for building the ROM
            IODirectory originalDirectoryTree = new("original", new IODirectory[]
            {
                new("archives", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("overlay", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("bgm", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("vce", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
            }, Array.Empty<IOFile>());
            IODirectory srcDirectoryTree = new("src", new IODirectory[]
            {
                new("source", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("replSource", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("overlays", Array.Empty<IODirectory>(), new IOFile[]
                {
                    new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x")),
                    new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_overlay"), "Makefile"),
                }),
            },
            new IOFile[]
            {
                new(Path.Combine(project.BaseDirectory, "rom", "arm9.bin")),
                new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x")),
                new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_main"), "Makefile"),
            });
            IODirectory assetsDirectoryTree = new("assets", new IODirectory[]
            {
                new("data", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("events", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("graphics", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("misc", Array.Empty<IODirectory>(), new IOFile[] { new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "charset.json")) }),
                new("movie", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("scn", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
            }, Array.Empty<IOFile>());
            originalDirectoryTree.Create(project.BaseDirectory);
            originalDirectoryTree.Create(project.IterativeDirectory);
            srcDirectoryTree.Create(project.BaseDirectory);
            srcDirectoryTree.Create(project.IterativeDirectory);
            assetsDirectoryTree.Create(project.BaseDirectory);
            assetsDirectoryTree.Create(project.IterativeDirectory);

            // Copy out the files we need to build the ROM
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
            foreach (string file in Directory.GetFiles(sourceDirectory, filter))
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
