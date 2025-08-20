using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HaroohieClub.NitroPacker;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib;

public static class Build
{
    public static bool BuildIterative(Project project, ConfigUser configUser, ILogger log, IProgressTracker tracker)
    {
        bool result = DoBuild(project.IterativeDirectory, project, configUser, log, tracker);
        CopyToArchivesToIterativeOriginal(Path.Combine(project.IterativeDirectory, "rom", "data"),
            Path.Combine(project.IterativeDirectory, "original", "archives"), project, log, tracker);
        ReplicateProjectSettingsAndBanner(Path.Combine(project.IterativeDirectory, "rom"),
            Path.Combine(project.BaseDirectory, "rom"), project.Name, log, tracker);
        if (result)
        {
            CleanIterative(project, log, tracker);
        }
        return result;
    }

    public static bool BuildBase(Project project, ConfigUser configUser, ILogger log, IProgressTracker tracker)
    {
        bool result = DoBuild(project.BaseDirectory, project, configUser, log, tracker);
        CopyToArchivesToIterativeOriginal(Path.Combine(project.BaseDirectory, "rom", "data"),
            Path.Combine(project.IterativeDirectory, "original", "archives"), project, log, tracker);
        ReplicateProjectSettingsAndBanner(Path.Combine(project.BaseDirectory, "rom"),
            Path.Combine(project.IterativeDirectory, "rom"), project.Name, log, tracker);
        if (result)
        {
            CleanIterative(project, log, tracker);
        }
        return result;
    }

    private static void CleanIterative(Project project, ILogger log, IProgressTracker tracker)
    {
        string[] preservedFiles = [];
        string[] cleanableFiles = Directory.GetFiles(Path.Combine(project.IterativeDirectory, "assets"), "*", SearchOption.AllDirectories);
        tracker.Focus("BuildCleaningIterativeDirectory", cleanableFiles.Length);
        foreach (string file in cleanableFiles)
        {
            if (!preservedFiles.Contains(Path.GetFileName(file)))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException exc)
                {
                    log.LogException("Failed to clean iterative directory", exc);
                }
            }
            tracker.Finished++;
        }
    }

    private static bool DoBuild(string directory, Project project, ConfigUser configUser, ILogger log, IProgressTracker tracker)
    {
        // Export includes
        StringBuilder commandsIncSb = new();
        foreach (ScriptCommand command in EventFile.CommandsAvailable)
        {
            commandsIncSb.AppendLine(command.GetMacro());
        }

        tracker.Focus(project.Localize("ProjectLoadLoadingDatBin"), 3);
        var dat = ArchiveFile<DataFile>.FromFile(Path.Combine(directory, "original", "archives", "dat.bin"), log);
        tracker.Finished++;
        tracker.CurrentlyLoading = project.Localize("ProjectLoadLoadingEvtBin");
        var evt = ArchiveFile<EventFile>.FromFile(Path.Combine(directory, "original", "archives", "evt.bin"), log);
        tracker.Finished++;
        tracker.CurrentlyLoading = project.Localize("ProjectLoadLoadingGrpBin");
        var grp = ArchiveFile<GraphicsFile>.FromFile(Path.Combine(directory, "original", "archives", "grp.bin"), log);

        if (dat is null || evt is null || grp is null)
        {
            log.LogError(project.Localize("ErrorArchivesNull"));
            return false;
        }

        tracker.Focus(project.Localize("BuildWritingIncludesProgressMessage"), 4);
        try
        {
            File.WriteAllText(Path.Combine(directory, "COMMANDS.INC"), commandsIncSb.ToString());
            File.WriteAllText(Path.Combine(directory, "DATBIN.INC"), dat.GetSourceInclude());
            File.WriteAllText(Path.Combine(directory, "EVTBIN.INC"), evt.GetSourceInclude());
            File.WriteAllText(Path.Combine(directory, "GRPBIN.INC"), grp.GetSourceInclude());
        }
        catch (IOException exc)
        {
            log.LogException(project.Localize("ErrorFailedWritingIncludeFiles"), exc);
            return false;
        }
        tracker.Finished += 4;

        // Replace files
        string[] files = Directory.GetFiles(Path.Combine(directory, "assets"), "*.*", SearchOption.AllDirectories);
        tracker.Focus(project.Localize("BuildReplacingFilesProgressMessage"), files.Length);
        foreach (string file in files)
        {
            if (int.TryParse(Path.GetFileNameWithoutExtension(file).Split('_')[0], NumberStyles.HexNumber, new CultureInfo("en-US"), out int index) || Path.GetFileName(file).StartsWith("new", StringComparison.OrdinalIgnoreCase))
            {
                if (index > 0)
                {
                    if (Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        ReplaceSingleGraphicsFile(grp, file, index, project.Localize, log);
                    }
                    else if (Path.GetExtension(file).Equals(".gi", StringComparison.OrdinalIgnoreCase))
                    {
                        // ignore graphics info files as they will be handled by the PNGs above
                    }
                    else if (Path.GetExtension(file).Equals(".scr", StringComparison.OrdinalIgnoreCase))
                    {
                        ReplaceSingleScreenGraphicsFile(grp, file, index, project.Localize, log);
                    }
                    else if (Path.GetExtension(file).Equals(".lay", StringComparison.OrdinalIgnoreCase))
                    {
                        ReplaceSingleLayoutGraphicsFile(grp, file, index, project.Localize, log);
                    }
                    else if (Path.GetExtension(file).Equals(".s", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(configUser.SysConfig.LlvmPath))
                        {
                            log.LogError("LLVM must be supplied in order to build!");
                            return false;
                        }
                        if (file.Contains("events"))
                        {
                            ReplaceSingleSourceFile(evt, file, index, configUser.SysConfig.LlvmPath, directory, project.Localize, log);
                        }
                        else if (file.Contains("data"))
                        {
                            ReplaceSingleSourceFile(dat, file, index, configUser.SysConfig.LlvmPath, directory, project.Localize, log);
                        }
                        else
                        {
                            log.LogWarning($"Source file found at '{file}', outside of data and events directory; skipping...");
                        }
                    }
                    else if (Path.GetExtension(file).Equals(".bna", StringComparison.OrdinalIgnoreCase))
                    {
                        ReplaceSingleBinaryFile(grp, file, index, project.Localize, log);
                    }
                    else
                    {
                        log.LogError(string.Format(project.Localize("BuildUnsureWhatToDoWithFileMessage"), Path.GetFileName(file)));
                        return false;
                    }
                }
                else
                {
                    //AddNewFile(archive, filePath, log);
                }
            }
            tracker.Finished++;
        }

        // Save files to disk
        tracker.Focus("BuildWritingReplacedArchivesProgressMessage", 3);
        if (!IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "dat.bin"), dat?.GetBytes(), log))
        {
            return false;
        }
        if (!IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "evt.bin"), evt?.GetBytes(), log))
        {
            return false;
        }
        if (!IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "grp.bin"), grp?.GetBytes(), log))
        {
            return false;
        }
        tracker.Finished += 3;

        // Save project file to disk
        string ndsProjectFile = Path.Combine(directory, "rom", $"{project.Name}.json");
        tracker.Focus(project.Localize("BuildWritingNPProjectFile"), 1);
        try
        {
            project.Settings.File.Serialize(ndsProjectFile);
        }
        catch (IOException exc)
        {
            log.LogException(project.Localize("ErrorFailedWritingNPProjectFile"), exc);
            return false;
        }
        tracker.Finished++;

        tracker.Focus(project.Localize("BuildPackingRomProgressMessage"), 1);
        try
        {
            NdsProjectFile.Pack(Path.Combine(project.MainDirectory, $"{project.Name}.nds"), ndsProjectFile);
        }
        catch (Exception exc)
        {
            log.LogException(project.Localize("BuildNitroPackerFailedPacking"), exc);
            return false;
        }
        tracker.Finished++;

        return true;
    }

    private static void CopyToArchivesToIterativeOriginal(string newDataDir, string iterativeOriginalDir, Project project, ILogger log, IProgressTracker tracker)
    {
        tracker.Focus(project.Localize("BuildCopyingArchivesMessage"), 4);
        try
        {
            File.Copy(Path.Combine(newDataDir, "dat.bin"), Path.Combine(iterativeOriginalDir, "dat.bin"), overwrite: true);
            tracker.Finished++;
            File.Copy(Path.Combine(newDataDir, "evt.bin"), Path.Combine(iterativeOriginalDir, "evt.bin"), overwrite: true);
            tracker.Finished++;
            File.Copy(Path.Combine(newDataDir, "grp.bin"), Path.Combine(iterativeOriginalDir, "grp.bin"), overwrite: true);
            tracker.Finished++;
            File.Copy(Path.Combine(newDataDir, "snd.bin"), Path.Combine(iterativeOriginalDir, "snd.bin"), overwrite: true);
            tracker.Finished++;
        }
        catch (IOException exc)
        {
            log.LogException(project.Localize("ErrorFailedCopyingArchivesToIterativeDir"), exc);
        }
    }

    private static void ReplicateProjectSettingsAndBanner(string sourceDir, string targetDir, string projectName, ILogger log, IProgressTracker tracker)
    {
        tracker.Focus("Replicating Project Settings & Banner", 2);
        try
        {
            File.Copy(Path.Combine(sourceDir, $"{projectName}.json"), Path.Combine(targetDir, $"{projectName}.json"), overwrite: true);
            tracker.Finished++;
            File.Copy(Path.Combine(sourceDir, "banner.bin"), Path.Combine(targetDir, "banner.bin"), overwrite: true);
            tracker.Finished++;
        }
        catch (IOException exc)
        {
            log.LogException($"Failed to copy project settings and/or banner to {targetDir}", exc);
        }
    }

    private static void ReplaceSingleGraphicsFile(ArchiveFile<GraphicsFile> grp, string filePath, int index, Func<string, string> localize, ILogger log)
    {
        try
        {
            GraphicsFile grpFile = grp.GetFileByIndex(index);

            if (index == 0xE50)
            {
                grpFile.InitializeFontFile();
            }

            GraphicInfo graphicInfo = JsonSerializer.Deserialize<GraphicInfo>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(filePath), $"{Path.GetFileNameWithoutExtension(filePath)}.gi")));

            graphicInfo.Set(grpFile);
            grpFile.SetImage(filePath, newSize: true);

            grp.Files[grp.Files.IndexOf(grpFile)] = grpFile;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingGraphicsFile"), index, filePath), ex);
        }
    }

    private static void ReplaceSingleScreenGraphicsFile(ArchiveFile<GraphicsFile> grp, string filePath, int index, Func<string, string> localize, ILogger log)
    {
        try
        {
            GraphicsFile screenFile = grp.GetFileByIndex(index);

            screenFile.ScreenData = JsonSerializer.Deserialize<List<GraphicsFile.ScreenDataEntry>>(File.ReadAllText(filePath));
            screenFile.Edited = true;

            grp.Files[grp.Files.IndexOf(screenFile)] = screenFile;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingGraphicsFile"), index, filePath), ex);
        }
    }

    private static void ReplaceSingleLayoutGraphicsFile(ArchiveFile<GraphicsFile> grp, string filePath, int index, Func<string, string> localize, ILogger log)
    {
        try
        {
            GraphicsFile layoutFile = grp.GetFileByIndex(index);

            var layoutEntries = JsonSerializer.Deserialize<List<LayoutEntry>>(File.ReadAllText(filePath), Project.SERIALIZER_OPTIONS);
            layoutFile.LayoutEntries = layoutEntries;
            layoutFile.Data = [.. layoutFile.GetBytes()];
            layoutFile.Edited = true;

            grp.Files[grp.Files.IndexOf(layoutFile)] = layoutFile;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingGraphicsFile"), index, filePath), ex);
        }
    }

    private static bool ReplaceSingleSourceFile(ArchiveFile<EventFile> archive, string filePath, int index, string llvm, string workingDirectory, Func<string, string> localize, ILogger log)
    {
        try
        {
            (string objFile, string binFile) = CompileSourceFile(filePath, llvm, workingDirectory, localize, log);
            if (!File.Exists(binFile))
            {
                log.LogError(string.Format(localize("BuildErrorCompiledFileNotExist"), binFile));
                return false;
            }
            ReplaceSingleFile(archive, binFile, index, localize, log);
            File.Delete(objFile);
            File.Delete(binFile);
            return true;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingEvtSourceFile"), index, filePath), ex);
            return false;
        }
    }
    private static bool ReplaceSingleSourceFile(ArchiveFile<DataFile> archive, string filePath, int index, string llvm, string workingDirectory, Func<string, string> localize, ILogger log)
    {
        try
        {
            (string objFile, string binFile) = CompileSourceFile(filePath, llvm, workingDirectory, localize, log);
            if (!File.Exists(binFile))
            {
                log.LogError(string.Format(localize("BuildErrorCompiledFileNotExist"), binFile));
                return false;
            }
            ReplaceSingleFile(archive, binFile, index, localize, log);
            File.Delete(objFile);
            File.Delete(binFile);
            return true;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingDatFile"), index, filePath), ex);
            return false;
        }
    }

    private static (string, string) CompileSourceFile(string filePath, string llvm, string workingDirectory, Func<string, string> localize, ILogger log)
    {
        string exeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        string objFile = $"{Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileNameWithoutExtension(filePath))}.o";
        string binFile = $"{Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileNameWithoutExtension(filePath))}.bin";
        ProcessStartInfo clangStartInfo = new(Path.Combine(llvm, "bin", $"clang{exeExtension}"), $"-target armv5-none-eabi -nodefaultlibs -I. -static -c -o \"{objFile}\" \"{filePath}\"")
        {
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (!File.Exists(clangStartInfo.FileName))
        {
            log.LogError(string.Format(localize("ClangNotFoundMessage"), clangStartInfo.FileName));
            return (string.Empty, string.Empty);
        }
        log.Log($"Compiling '{filePath}' to '{objFile}' with '{clangStartInfo.FileName}'...");
        Process clang = new() { StartInfo = clangStartInfo };
        clang.OutputDataReceived += (_, e) => log.Log(e.Data);
        clang.ErrorDataReceived += (_, e) => log.LogWarning(e.Data);
        clang.Start();
        clang.WaitForExit();
        Task.Delay(50); // ensures process is actually complete
        if (clang.ExitCode != 0)
        {
            log.LogError(string.Format(localize("ClangExitedWithFailureMessage"), clang.ExitCode));
            return (string.Empty, string.Empty);
        }

        ProcessStartInfo objcopyStartInfo = new(Path.Combine(llvm, "bin", $"llvm-objcopy{exeExtension}"), $"-O binary \"{objFile}\" \"{binFile}\"")
        {
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (!File.Exists(objcopyStartInfo.FileName))
        {
            log.LogError(string.Format(localize("ObjcopyNotFoundMessage"), objcopyStartInfo.FileName));
            return (string.Empty, string.Empty);
        }
        log.Log($"Objcopying '{objFile}' to '{binFile}' with '{objcopyStartInfo.FileName}'...");
        Process objcopy = new() { StartInfo = objcopyStartInfo };
        objcopy.OutputDataReceived += (_, e) => log.Log(e.Data);
        objcopy.ErrorDataReceived += (_, e) => log.LogWarning(e.Data);
        objcopy.Start();
        objcopy.WaitForExit();
        Task.Delay(50); // ensures process is actually complete
        if (objcopy.ExitCode != 0)
        {
            log.LogError(string.Format(localize("ObjcopyExitedWithFailureMessage"), objcopy.ExitCode));
            return (string.Empty, string.Empty);
        }

        return (objFile, binFile);
    }

    private static void ReplaceSingleFile(ArchiveFile<EventFile> archive, string filePath, int index, Func<string, string> localize, ILogger log)
    {
        try
        {
            EventFile file = archive.GetFileByIndex(index);
            file.Data = [.. File.ReadAllBytes(filePath)];
            file.Edited = true;
            archive.Files[archive.Files.IndexOf(file)] = file;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingEvtFile"), index, filePath), ex);
        }
    }
    private static void ReplaceSingleFile(ArchiveFile<DataFile> archive, string filePath, int index, Func<string, string> localize, ILogger log)
    {
        try
        {
            DataFile file = archive.GetFileByIndex(index);
            file.Data = [.. File.ReadAllBytes(filePath)];
            file.Edited = true;
            archive.Files[archive.Files.IndexOf(file)] = file;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingDatFile"), index, filePath), ex);
        }
    }
    private static void ReplaceSingleBinaryFile(ArchiveFile<GraphicsFile> archive, string filePath, int index, Func<string, string> localize, ILogger log)
    {
        try
        {
            GraphicsFile file = archive.GetFileByIndex(index);
            GraphicsFile newFile = new()
            {
                Name = file.Name,
                Index = file.Index,
            };
            newFile.Initialize(File.ReadAllBytes(filePath), file.Offset, log);
            newFile.Edited = true;
            archive.Files[archive.Files.IndexOf(file)] = newFile;
        }
        catch (Exception ex)
        {
            log.LogException(string.Format(localize("ErrorFailedReplacingAnimationFile"), index, filePath), ex);
        }
    }
}
