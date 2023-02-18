using HaruhiChokuretsuLib.Util;
using HaroohieClub.NitroPacker.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using SerialLoops.Lib.Util;

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

        public static void OpenRom(Project project, string romPath, bool includeFontHack, IProgressTracker tracker)
        {
            // Unpack the ROM, creating the two project directories
            tracker.Focus("Creating Directories", 8);
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.BaseDirectory, "rom"));
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.IterativeDirectory, "rom"));
            tracker.Finished += 2;

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
                new("misc", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("movie", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
                new("scn", Array.Empty<IODirectory>(), Array.Empty<IOFile>()),
            }, Array.Empty<IOFile>());
            
            assetsDirectoryTree.Subdirectories.First(f => f.Name == "misc").Files = new IOFile[] { new IOFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "charset.json")) };

            originalDirectoryTree.Create(project.BaseDirectory);
            originalDirectoryTree.Create(project.IterativeDirectory);
            srcDirectoryTree.Create(project.BaseDirectory);
            srcDirectoryTree.Create(project.IterativeDirectory);
            assetsDirectoryTree.Create(project.BaseDirectory);
            assetsDirectoryTree.Create(project.IterativeDirectory);
            tracker.Finished += 6;

            if (includeFontHack)
            {
                tracker.Focus("Applying Hacks", 1);
                SetUpLocalizedHacks(project);
                tracker.Finished++;
            }

            // Copy out the files we need to build the ROM
            tracker.Focus("Copying Files", 4);
            CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data"), Path.Combine(project.BaseDirectory, "original", "archives"), "*.bin");
            tracker.Finished++;

            CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data"), Path.Combine(project.IterativeDirectory, "original", "archives"), "*.bin");
            tracker.Finished++;

            CopyFiles(Path.Combine(project.BaseDirectory, "rom", "overlay"), Path.Combine(project.BaseDirectory, "original", "overlay"));
            tracker.Finished++;

            CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "overlay"), Path.Combine(project.IterativeDirectory, "original", "overlay"));
            tracker.Finished++;

            // We conditionalize these so we can test on a non-copyrighted ROM; this should always be true with real data
            if (Directory.Exists(Path.Combine(project.BaseDirectory, "rom", "data", "bgm")))
            {
                CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data", "bgm"), Path.Combine(project.BaseDirectory, "original", "bgm"));
                CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data", "bgm"), Path.Combine(project.IterativeDirectory, "original", "bgm"));
                CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data", "vce"), Path.Combine(project.BaseDirectory, "original", "vce"));
                CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data", "vce"), Path.Combine(project.IterativeDirectory, "original", "vce"));
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

        public static bool WriteStringFile(string relativePath, string src, Project project, ILogger log)
        {
            return WriteStringFile(Path.Combine(project.IterativeDirectory, relativePath), src, log) && WriteStringFile(Path.Combine(project.BaseDirectory, relativePath), src, log);
        }

        public static bool WriteBinaryFile(string relativePath, byte[] bytes, Project project, ILogger log)
        {
            return WriteBinaryFile(Path.Combine(project.IterativeDirectory, relativePath), bytes, log) && WriteBinaryFile(Path.Combine(project.BaseDirectory, relativePath), bytes, log);
        }

        public static bool TryReadStringFile(string file, out string content, ILogger log)
        {
            try
            {
                content = File.ReadAllText(file);
                return true;
            }
            catch (IOException exc)
            {
                log.LogError($"Exception occurred while reading file '{file}' from disk.\n{exc.Message}\n\n{exc.StackTrace}");
                content = string.Empty;
                return false;
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

        public static bool WriteBinaryFile(string file, byte[] bytes, ILogger log)
        {
            try
            {
                File.WriteAllBytes(file, bytes);
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
