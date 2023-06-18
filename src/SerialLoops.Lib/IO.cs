﻿using HaroohieClub.NitroPacker.Core;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using System;
using System.Collections.Generic;
using System.IO;

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

        public static void OpenRom(Project project, string romPath, IProgressTracker tracker)
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
            },
            new IOFile[]
            {
                new(Path.Combine(project.BaseDirectory, "rom", $"{project.Name}.xml")),
            });
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

            originalDirectoryTree.Create(project.BaseDirectory);
            originalDirectoryTree.Create(project.IterativeDirectory);
            srcDirectoryTree.Create(project.BaseDirectory);
            srcDirectoryTree.Create(project.IterativeDirectory);
            assetsDirectoryTree.Create(project.BaseDirectory);
            assetsDirectoryTree.Create(project.IterativeDirectory);
            tracker.Finished += 6;

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

        public static void CopyFileToDirectories(Project project, string sourceFile, string relativePath)
        {
            string baseFile = Path.Combine(project.BaseDirectory, relativePath);
            string iterativeFile = Path.Combine(project.IterativeDirectory, relativePath);

            if (!Directory.Exists(Path.GetDirectoryName(baseFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(baseFile));
            }
            if (!Directory.Exists(Path.GetDirectoryName(iterativeFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(iterativeFile));
            }

            File.Copy(sourceFile, baseFile, true);
            File.Copy(sourceFile, iterativeFile, true);
        }

        public static void DeleteFiles(Project project, IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                File.Delete(Path.Combine(project.IterativeDirectory, file));
                File.Delete(Path.Combine(project.BaseDirectory, file));
            }
        }

        public static void CopyFiles(string sourceDirectory, string destinationDirectory, string filter = "*")
        {
            foreach (string file in Directory.GetFiles(sourceDirectory, filter))
            {
                File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), overwrite: true);
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
