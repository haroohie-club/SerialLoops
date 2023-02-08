using HaroohieClub.NitroPacker.Core;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
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
        public static async Task<bool> BuildIterative(Project project, Config config, ILogger log)
        {
            bool result = await DoBuild(project.IterativeDirectory, project, config, log);
            CopyToArchivesToIterativeOriginal(Path.Combine(project.IterativeDirectory, "rom", "data"),
                Path.Combine(project.IterativeDirectory, "original", "archives"), log);
            if (result)
            {
                CleanIterative(project, log);
            }
            return result;
        }

        public static async Task<bool> BuildBase(Project project, Config config, ILogger log)
        {
            bool result = await DoBuild(project.BaseDirectory, project, config, log);
            CopyToArchivesToIterativeOriginal(Path.Combine(project.BaseDirectory, "rom", "data"),
                Path.Combine(project.IterativeDirectory, "original", "archives"), log);
            if (result)
            {
                CleanIterative(project, log);
            }
            return result;
        }

        private static void CleanIterative(Project project, ILogger log)
        {
            string[] preservedFiles = new string[] { "charset.json", "e50_newsize.png" };
            foreach (string file in Directory.GetFiles(Path.Combine(project.IterativeDirectory, "assets"), "*", SearchOption.AllDirectories))
            {
                if (!preservedFiles.Contains(Path.GetFileName(file)))
                {
                    File.Delete(file);
                }
            }
        }

        private static async Task<bool> DoBuild(string directory, Project project, Config config, ILogger log)
        {
            // Export includes
            StringBuilder commandsIncSb = new();
            foreach (ScriptCommand command in EventFile.CommandsAvailable)
            {
                commandsIncSb.AppendLine(command.GetMacro());
            }

            var dat = ArchiveFile<DataFile>.FromFile(Path.Combine(directory, "original", "archives", "dat.bin"), log);
            var evt = ArchiveFile<EventFile>.FromFile(Path.Combine(directory, "original", "archives", "evt.bin"), log);
            var grp = ArchiveFile<GraphicsFile>.FromFile(Path.Combine(directory, "original", "archives", "grp.bin"), log);

            File.WriteAllText(Path.Combine(directory, "COMMANDS.INC"), commandsIncSb.ToString());
            File.WriteAllText(Path.Combine(directory, "DATBIN.INC"), dat.GetSourceInclude());
            File.WriteAllText(Path.Combine(directory, "EVTBIN.INC"), evt.GetSourceInclude());
            File.WriteAllText(Path.Combine(directory, "GRPBIN.INC"), grp.GetSourceInclude());

            // Replace files
            foreach (string file in Directory.GetFiles(Path.Combine(directory, "assets"), "*.*", SearchOption.AllDirectories))
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
            }

            // Save files to disk
            IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "dat.bin"), dat.GetBytes(), log);
            IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "evt.bin"), evt.GetBytes(), log);
            IO.WriteBinaryFile(Path.Combine(directory, "rom", "data", "grp.bin"), grp.GetBytes(), log);

            NdsProjectFile.Pack(Path.Combine(project.MainDirectory, $"{project.Name}.nds"), Path.Combine(directory, "rom", $"{project.Name}.xml"));
            return true;
        }

        private static void CopyToArchivesToIterativeOriginal(string newDataDir, string iterativeOriginalDir, ILogger log)
        {
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

        private static async Task ReplaceSingleSourceFileAsync(ArchiveFile<EventFile> archive, string filePath, int index, string devkitArm, string workingDirectory, ILogger log)
        {
            (string objFile, string binFile) = await CompileSourceFileAsync(filePath, devkitArm, workingDirectory, log);
            ReplaceSingleFile(archive, binFile, index);
            File.Delete(objFile);
            File.Delete(binFile);
        }
        private static async Task ReplaceSingleSourceFileAsync(ArchiveFile<DataFile> archive, string filePath, int index, string devkitArm, string workingDirectory, ILogger log)
        {
            (string objFile, string binFile) = await CompileSourceFileAsync(filePath, devkitArm, workingDirectory, log);
            ReplaceSingleFile(archive, binFile, index);
            File.Delete(objFile);
            File.Delete(binFile);
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
            Process gcc = new() { StartInfo = gccStartInfo };
            gcc.OutputDataReceived += (object sender, DataReceivedEventArgs e) => log.Log(e.Data);
            gcc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data, lookForErrors: true);
            gcc.Start();
            await gcc.WaitForExitAsync();
            await Task.Delay(50); // ensures process is actually complete
            ProcessStartInfo objcopyStartInfo = new(Path.Combine(devkitArm, "bin", $"arm-none-eabi-objcopy{exeExtension}"), $"-O binary \"{objFile}\" \"{binFile}")
            {
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };
            Process objcopy = new() { StartInfo = objcopyStartInfo };
            objcopy.OutputDataReceived += (object sender, DataReceivedEventArgs e) => log.Log(e.Data);
            objcopy.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data, lookForErrors: true);
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
