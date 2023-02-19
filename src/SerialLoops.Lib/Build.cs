using HaroohieClub.NitroPacker.Core;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerialLoops.Lib
{
    public static class Build
    {
        public static async Task<bool> BuildIterative(Project project, Config config, ILogger log, IProgressTracker tracker)
        {
            bool result = await DoBuild(project.IterativeDirectory, project, config, log, tracker);
            CopyToArchivesToIterativeOriginal(Path.Combine(project.IterativeDirectory, "rom", "data"),
                Path.Combine(project.IterativeDirectory, "original", "archives"), log, tracker);
            if (result)
            {
                CleanIterative(project, log, tracker);
            }
            return result;
        }

        public static async Task<bool> BuildBase(Project project, Config config, ILogger log, IProgressTracker tracker)
        {
            bool result = await DoBuild(project.BaseDirectory, project, config, log, tracker);
            CopyToArchivesToIterativeOriginal(Path.Combine(project.BaseDirectory, "rom", "data"),
                Path.Combine(project.IterativeDirectory, "original", "archives"), log, tracker);
            if (result)
            {
                CleanIterative(project, log, tracker);
            }
            return result;
        }

        private static void CleanIterative(Project project, ILogger log, IProgressTracker tracker)
        {
            string[] preservedFiles = Array.Empty<string>();
            string[] cleanableFiles = Directory.GetFiles(Path.Combine(project.IterativeDirectory, "assets"), "*", SearchOption.AllDirectories);
            tracker.Focus("Cleaning Iterative Directory", cleanableFiles.Length);
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
                        log.LogError($"Failed to clean iterative directory: {exc.Message}\n\n{exc.StackTrace}");
                    }
                }
                tracker.Finished++;
            }
        }

        private static async Task<bool> DoBuild(string directory, Project project, Config config, ILogger log, IProgressTracker tracker)
        {
            // Export includes
            StringBuilder commandsIncSb = new();
            foreach (ScriptCommand command in EventFile.CommandsAvailable)
            {
                commandsIncSb.AppendLine(command.GetMacro());
            }

            tracker.Focus("Compressing Archives (dat.bin)", 3);
            var dat = ArchiveFile<DataFile>.FromFile(Path.Combine(directory, "original", "archives", "dat.bin"), log);
            tracker.Finished++;
            tracker.CurrentlyLoading = "Compressing Archives (evt.bin)";
            var evt = ArchiveFile<EventFile>.FromFile(Path.Combine(directory, "original", "archives", "evt.bin"), log);
            tracker.Finished++;
            tracker.CurrentlyLoading = "Compressing Archives (grp.bin)";
            var grp = ArchiveFile<GraphicsFile>.FromFile(Path.Combine(directory, "original", "archives", "grp.bin"), log);

            if (dat is null || evt is null || grp is null)
            {
                log.LogError("One or more archives is null.");
                return false;
            }

            tracker.Focus("Writing Includes", 4);
            try
            {
                File.WriteAllText(Path.Combine(directory, "COMMANDS.INC"), commandsIncSb.ToString());
                File.WriteAllText(Path.Combine(directory, "DATBIN.INC"), dat.GetSourceInclude());
                File.WriteAllText(Path.Combine(directory, "EVTBIN.INC"), evt.GetSourceInclude());
                File.WriteAllText(Path.Combine(directory, "GRPBIN.INC"), grp.GetSourceInclude());
            }
            catch (IOException exc)
            {
                log.LogError($"Failed to write include files to disk: {exc.Message}\n\n{exc.StackTrace}");
                return false;
            }
            tracker.Finished+= 4;

            // Replace files
            string[] files = Directory.GetFiles(Path.Combine(directory, "assets"), "*.*", SearchOption.AllDirectories);
            tracker.Focus("Replacing Files", files.Length);
            foreach (string file in files)
            {
                if (int.TryParse(Path.GetFileNameWithoutExtension(file).Split('_')[0], NumberStyles.HexNumber, new CultureInfo("en-US"), out int index) || Path.GetFileName(file).StartsWith("new", StringComparison.OrdinalIgnoreCase))
                {
                    if (index > 0)
                    {
                        if (Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            ReplaceSingleGraphicsFile(grp, file, index, new());
                        }
                        else if (Path.GetExtension(file).Equals(".s", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrEmpty(config.DevkitArmPath))
                            {
                                log.LogError("DevkitARM must be supplied in order to build");
                                return false;
                            }
                            if (file.Contains("events"))
                            {
                                await ReplaceSingleSourceFileAsync(evt, file, index, config.DevkitArmPath, directory, log);
                            }
                            else if (file.Contains("data"))
                            {
                                await ReplaceSingleSourceFileAsync(dat, file, index, config.DevkitArmPath, directory, log);
                            }
                            else
                            {
                                log.LogWarning($"Source file found at '{file}', outside of data and events directory; skipping...");
                            }
                        }
                        else
                        {
                            log.LogError($"Unsure what to do with file '{Path.GetFileName(file)}'");
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
            tracker.Focus("Writing Replaced Archives", 3);
            if (!IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "dat.bin"), dat.GetBytes(), log))
            {
                return false;
            }
            if (!IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "evt.bin"), evt.GetBytes(), log))
            {
                return false;
            }
            if (!IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "grp.bin"), grp.GetBytes(), log))
            {
                return false;
            }
            tracker.Finished+= 3;

            tracker.Focus("Packing ROM", 1);
            try
            {
                NdsProjectFile.Pack(Path.Combine(project.MainDirectory, $"{project.Name}.nds"), Path.Combine(directory, "rom", $"{project.Name}.xml"));
            }
            catch (Exception exc)
            {
                log.LogError($"NitroPacker failed to pack ROM with exception '{exc.Message}'\n\n{exc.StackTrace}");
                return false;
            }
            tracker.Finished++;
            return true;
        }

        private static void CopyToArchivesToIterativeOriginal(string newDataDir, string iterativeOriginalDir, ILogger log, IProgressTracker tracker)
        {
            tracker.Focus("Copying Archives to Iterative Originals", 3);
            try
            {
                File.Copy(Path.Combine(newDataDir, "dat.bin"), Path.Combine(iterativeOriginalDir, "dat.bin"), overwrite: true);
                File.Copy(Path.Combine(newDataDir, "evt.bin"), Path.Combine(iterativeOriginalDir, "evt.bin"), overwrite: true);
                File.Copy(Path.Combine(newDataDir, "grp.bin"), Path.Combine(iterativeOriginalDir, "grp.bin"), overwrite: true);
            }
            catch (IOException exc)
            {
                log.LogError($"Failed to copy newly built archives to the iterative originals directory.\n{exc.Message}\n\n{exc.StackTrace}");
            }
            tracker.Finished+= 3;
        }

        private static void ReplaceSingleGraphicsFile(ArchiveFile<GraphicsFile> grp, string filePath, int index, Dictionary<int, List<SKColor>> sharedPalettes)
        {
            GraphicsFile grpFile = grp.Files.FirstOrDefault(f => f.Index == index);

            if (index == 0xE50)
            {
                grpFile.InitializeFontFile();
            }

            int transparentIndex = -1;
            Match transparentIndexMatch = Regex.Match(filePath, @"tidx(?<transparentIndex>\d+)", RegexOptions.IgnoreCase);
            if (transparentIndexMatch.Success)
            {
                transparentIndex = int.Parse(transparentIndexMatch.Groups["transparentIndex"].Value);
            }
            Match sharedPaletteMatch = Regex.Match(filePath, @"sharedpal(?<index>\d+)", RegexOptions.IgnoreCase);
            if (sharedPaletteMatch.Success)
            {
                grpFile.SetPalette(sharedPalettes[int.Parse(sharedPaletteMatch.Groups["index"].Value)]);
            }
            bool newSize = filePath.Contains("newsize");

            grpFile.SetImage(filePath, setPalette: Path.GetFileNameWithoutExtension(filePath).Contains("newpal", StringComparison.OrdinalIgnoreCase), transparentIndex: transparentIndex, newSize: newSize);

            grp.Files[grp.Files.IndexOf(grpFile)] = grpFile;
        }

        private static async Task<bool> ReplaceSingleSourceFileAsync(ArchiveFile<EventFile> archive, string filePath, int index, string devkitArm, string workingDirectory, ILogger log)
        {
            (string objFile, string binFile) = await CompileSourceFileAsync(filePath, devkitArm, workingDirectory, log);
            if (!File.Exists(binFile))
            {
                log.LogError($"Compiled file {binFile} does not exist!");
                return false;
            }
            ReplaceSingleFile(archive, binFile, index);
            File.Delete(objFile);
            File.Delete(binFile);
            return true;
        }
        private static async Task<bool> ReplaceSingleSourceFileAsync(ArchiveFile<DataFile> archive, string filePath, int index, string devkitArm, string workingDirectory, ILogger log)
        {
            (string objFile, string binFile) = await CompileSourceFileAsync(filePath, devkitArm, workingDirectory, log);
            if (!File.Exists(binFile))
            {
                log.LogError($"Compiled file {binFile} does not exist!");
                return false;
            }
            ReplaceSingleFile(archive, binFile, index);
            File.Delete(objFile);
            File.Delete(binFile);
            return true;
        }

        private static async Task<(string, string)> CompileSourceFileAsync(string filePath, string devkitArm, string workingDirectory, ILogger log)
        {
            string exeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

            string objFile = $"{Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))}.o";
            string binFile = $"{Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))}.bin";
            ProcessStartInfo gccStartInfo = new(Path.Combine(devkitArm, "bin", $"arm-none-eabi-gcc{exeExtension}"), $"-c -nostdlib -static \"{filePath}\" -o \"{objFile}")
            {
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            if (!File.Exists(gccStartInfo.FileName))
            {
                log.LogError($"gcc not found at '{gccStartInfo.FileName}'");
                return (string.Empty, string.Empty);
            }
            log.Log($"Compiling '{filePath}' to '{objFile}' with '{gccStartInfo.FileName}'...");
            Process gcc = new() { StartInfo = gccStartInfo };
            gcc.OutputDataReceived += (object sender, DataReceivedEventArgs e) => log.Log(e.Data);
            gcc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data);
            gcc.Start();
            await gcc.WaitForExitAsync();
            await Task.Delay(50); // ensures process is actually complete
            ProcessStartInfo objcopyStartInfo = new(Path.Combine(devkitArm, "bin", $"arm-none-eabi-objcopy{exeExtension}"), $"-O binary \"{objFile}\" \"{binFile}")
            {
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            if (!File.Exists(objcopyStartInfo.FileName))
            {
                log.LogError($"objcopy not found at '{objcopyStartInfo.FileName}'");
                return (string.Empty, string.Empty);
            }
            log.Log($"Objcopying '{objFile}' to '{binFile}' with '{objcopyStartInfo.FileName}'...");
            Process objcopy = new() { StartInfo = objcopyStartInfo };
            objcopy.OutputDataReceived += (object sender, DataReceivedEventArgs e) => log.Log(e.Data);
            objcopy.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data);
            objcopy.Start();
            await objcopy.WaitForExitAsync();
            await Task.Delay(50); // ensures process is actually complete

            return (objFile, binFile);
        }

        private static void ReplaceSingleFile(ArchiveFile<EventFile> archive, string filePath, int index)
        {
            EventFile file = archive.Files.FirstOrDefault(f => f.Index == index);
            file.Data = File.ReadAllBytes(filePath).ToList();
            file.Edited = true;
            archive.Files[archive.Files.IndexOf(file)] = file;
        }
        private static void ReplaceSingleFile(ArchiveFile<DataFile> archive, string filePath, int index)
        {
            DataFile file = archive.Files.FirstOrDefault(f => f.Index == index);
            file.Data = File.ReadAllBytes(filePath).ToList();
            file.Edited = true;
            archive.Files[archive.Files.IndexOf(file)] = file;
        }
    }
}
