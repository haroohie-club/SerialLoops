using System;
using System.Collections.Generic;
using System.IO;
using HaroohieClub.NitroPacker;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib;

public static class IO
{
    private class IODirectory(string name, IODirectory[] subdirectories, IOFile[] files)
    {
        public string Name { get; set; } = name;
        public IODirectory[] Subdirectories { get; set; } = subdirectories;
        public IOFile[] Files { get; set; } = files;

        public void Create(string basePath, ILogger log, Func<string, string> localize)
        {
            try
            {
                string dirPath = Path.Combine(basePath, Name);
                Directory.CreateDirectory(dirPath);
                foreach (IOFile file in Files)
                {
                    File.Copy(file.FilePath, Path.Combine(dirPath, file.Name));
                }
                foreach (IODirectory subdirectory in Subdirectories)
                {
                    subdirectory.Create(dirPath, log, localize);
                }
            }
            catch (Exception ex)
            {
                log.LogException(localize("ErrorFailedCreatingDir"), ex);
                log.LogWarning(basePath);
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

    public static void OpenRom(Project project, string romPath, ILogger log, IProgressTracker tracker)
    {
        // Unpack the ROM, creating the two project directories
        tracker.Focus("Creating Directories", 8);
        try
        {
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.BaseDirectory, "rom"));
            NdsProjectFile.Create(project.Name, romPath, Path.Combine(project.IterativeDirectory, "rom"));
        }
        catch (Exception ex)
        {
            log.LogException(project.Localize("ErrorFailedUnpackingRom"), ex);
            return;
        }
        tracker.Finished += 2;

        // Create our structure for building the ROM
        IODirectory originalDirectoryTree = new("original",
            [
                new("archives", [], []),
                new("overlay", [], []),
                new("bgm", [], []),
                new("vce", [], []),
            ],
            [
                new(Path.Combine(project.BaseDirectory, "rom", $"{project.Name}.json")),
            ]);
        IODirectory srcDirectoryTree = new("src",
            [
                new("source", [], []),
                new("replSource", [], []),
                new("overlays", [],
                [
                    new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x")),
                ]),
            ],
            [
                new(Path.Combine(project.BaseDirectory, "rom", "arm9.bin")),
                new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "linker.x")),
            ]);
        IODirectory assetsDirectoryTree = new("assets",
        [
            new("data", [], []),
            new("events", [], []),
            new("graphics", [], []),
            new("misc", [], []),
            new("movie", [], []),
            new("scn", [], []),
        ], []);

        originalDirectoryTree.Create(project.BaseDirectory, log, project.Localize);
        originalDirectoryTree.Create(project.IterativeDirectory, log, project.Localize);
        srcDirectoryTree.Create(project.BaseDirectory, log, project.Localize);
        srcDirectoryTree.Create(project.IterativeDirectory, log, project.Localize);
        assetsDirectoryTree.Create(project.BaseDirectory, log, project.Localize);
        assetsDirectoryTree.Create(project.IterativeDirectory, log, project.Localize);
        tracker.Finished += 6;

        // Copy out the files we need to build the ROM
        tracker.Focus(project.Localize("IOCopyingFilesMessage"), 4);
        CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data"), Path.Combine(project.BaseDirectory, "original", "archives"), log, "*.bin");
        tracker.Finished++;

        CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data"), Path.Combine(project.IterativeDirectory, "original", "archives"), log, "*.bin");
        tracker.Finished++;

        CopyFiles(Path.Combine(project.BaseDirectory, "rom", "overlay"), Path.Combine(project.BaseDirectory, "original", "overlay"), log);
        tracker.Finished++;

        CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "overlay"), Path.Combine(project.IterativeDirectory, "original", "overlay"), log);
        tracker.Finished++;

        // We conditionalize these so we can test on a non-copyrighted ROM; this should always be true with real data
        if (Directory.Exists(Path.Combine(project.BaseDirectory, "rom", "data", "bgm")))
        {
            CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data", "bgm"), Path.Combine(project.BaseDirectory, "original", "bgm"), log);
            CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data", "bgm"), Path.Combine(project.IterativeDirectory, "original", "bgm"), log);
            CopyFiles(Path.Combine(project.BaseDirectory, "rom", "data", "vce"), Path.Combine(project.BaseDirectory, "original", "vce"), log);
            CopyFiles(Path.Combine(project.IterativeDirectory, "rom", "data", "vce"), Path.Combine(project.IterativeDirectory, "original", "vce"), log);
        }
    }

    public static bool CopyDirectoryRecursively(string directory, string destination)
    {
        if (string.IsNullOrEmpty(destination) || !Directory.Exists(directory) || Directory.Exists(destination))
        {
            return false;
        }

        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(directory))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (string subdirectory in Directory.GetDirectories(directory))
        {
            CopyDirectoryRecursively(subdirectory, Path.Combine(destination, Path.GetFileName(subdirectory)));
        }

        return true;
    }

    public static void CopyFileToDirectories(Project project, string sourceFile, string relativePath, ILogger log)
    {
        string baseFile = Path.Combine(project.BaseDirectory, relativePath);
        string iterativeFile = Path.Combine(project.IterativeDirectory, relativePath);
        try
        {

            CreateDirectory(baseFile);
            CreateDirectory(iterativeFile);

            File.Copy(sourceFile, baseFile, true);
            File.Copy(sourceFile, iterativeFile, true);
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(project.Localize("ErrorFailedCopyingFile"), sourceFile, relativePath), ex);
        }
    }

    public static void DeleteFiles(Project project, IEnumerable<string> files, ILogger log)
    {
        foreach (string file in files)
        {
            try
            {
                File.Delete(Path.Combine(project.IterativeDirectory, file));
                File.Delete(Path.Combine(project.BaseDirectory, file));
            }
            catch (Exception ex)
            {
                log.LogException(string.Format(project.Localize("ErrorFailedDeletingFile"), file), ex);
            }
        }
    }

    public static void CopyFiles(string sourceDirectory, string destinationDirectory, ILogger log, string filter = "*", bool recursive = false)
    {
        if (recursive)
        {
            foreach (string dir in Directory.GetDirectories(sourceDirectory))
            {
                string newDir = Path.Combine(destinationDirectory, Path.GetFileName(dir));
                Directory.CreateDirectory(newDir);
                CopyFiles(dir, newDir, log, filter, recursive);
            }
        }
        foreach (string file in Directory.GetFiles(sourceDirectory, filter))
        {
            try
            {
                File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), overwrite: true);
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to copy file '{file}' from '{sourceDirectory}' to '{destinationDirectory}'", ex);
            }
        }
    }

    public static void DeleteFilesKeepDirectories(string sourceDirectory, ILogger log)
    {
        foreach (string directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            foreach (string file in Directory.GetFiles(directory))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    log.LogException($"Failed to delete file '{file}' from directory '{sourceDirectory}'", ex);
                }
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
        catch (IOException ex)
        {
            log.LogException($"Exception occurred while reading file '{file}' from disk.", ex);
            content = string.Empty;
            return false;
        }
    }

    public static bool WriteStringFile(string file, string str, ILogger log)
    {
        try
        {
            CreateDirectory(file);
            File.WriteAllText(file, str);
            return true;
        }
        catch (IOException ex)
        {
            log.LogException($"Exception occurred while writing file '{file}' to disk.", ex);
            return false;
        }
    }

    public static bool WriteBinaryFile(string file, byte[] bytes, ILogger log)
    {
        try
        {
            CreateDirectory(file);
            File.WriteAllBytes(file, bytes);
            return true;
        }
        catch (IOException ex)
        {
            log.LogException($"Exception occurred while writing file '{file}' to disk.", ex);
            return false;
        }
    }

    private static void CreateDirectory(string file)
    {
        if (!string.IsNullOrEmpty(Path.GetDirectoryName(file)) && !Directory.Exists(Path.GetDirectoryName(file)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        }
    }
}
