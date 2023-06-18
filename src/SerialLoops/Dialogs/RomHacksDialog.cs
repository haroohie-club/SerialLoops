using Eto.Forms;
using HaroohieClub.NitroPacker.Patcher.Nitro;
using HaroohieClub.NitroPacker.Patcher.Overlay;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SerialLoops.Dialogs
{
    public class RomHacksDialog : Dialog
    {
        private const int NUM_OVERLAYS = 26;

        public RomHacksDialog(Project project, Config config, ILogger log)
        {
            StackLayout hacksLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Size = new(200, 300),
            };

            foreach (AsmHack hack in config.Hacks)
            {
                CheckBox hackCheckBox = new() { Text = hack.Name, Checked = hack.Applied(project) };
                hacksLayout.Items.Add(hackCheckBox);
            }

            Button importButton = new()
            {
                Text = "Import Hack",
            };
            importButton.Click += (sender, args) =>
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = "Import a Hack"
                };
                openFileDialog.Filters.Add(new("Serialized ASM hack", ".slhack"));

                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    AsmHack hack = JsonSerializer.Deserialize<AsmHack>(File.ReadAllText(openFileDialog.FileName));

                    if (config.Hacks.Any(h => h.Files.Any(f => hack.Files.Contains(f))))
                    {
                        log.LogError($"Error: duplicate hack detected! A file with the same name as a file in this hack has already been imported.");
                        return;
                    }
                    else if (config.Hacks.Any(h => h.Name == hack.Name))
                    {
                        log.LogError($"Error: duplicate hack detected! A hack with the same name has already been imported.");
                        return;
                    }

                    foreach (HackFile file in hack.Files)
                    {
                        File.Copy(Path.Combine(Path.GetDirectoryName(openFileDialog.FileName), file.File), Path.Combine(config.HacksDirectory, file.File));
                    }

                    config.Hacks.Add(hack);
                    Lib.IO.WriteStringFile(Path.Combine(config.HacksDirectory, "hacks.json"), JsonSerializer.Serialize(config.Hacks), log);

                    CheckBox hackCheckBox = new() { Text = hack.Name, Checked = hack.Applied(project) };
                    hacksLayout.Items.Add(hackCheckBox);
                    hacksLayout.Invalidate();
                }
            };

            Button cancelButton = new()
            {
                Text = "Cancel",
            };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            Button saveButton = new()
            {
                Text = "Save",
            };
            saveButton.Click += (sender, args) =>
            {
                List<AsmHack> appliedHacks = new();
                foreach (CheckBox checkBox in hacksLayout.Items.Select(s => s.Control).Cast<CheckBox>())
                {
                    AsmHack hack = config.Hacks.First(f => f.Name == checkBox.Text);
                    bool applied = hack.Applied(project);

                    if (applied && (!checkBox.Checked ?? false))
                    {
                        hack.Revert(project, log);
                    }
                    else if (applied)
                    {
                        appliedHacks.Add(hack);
                    }
                    else
                    {
                        hack.Apply(project, config, log);
                        appliedHacks.Add(hack);
                    }
                }

                // Write the symbols file based on what the hacks say they need
                File.WriteAllLines(Path.Combine(project.BaseDirectory, "src", "symbols.x"), appliedHacks.SelectMany(h => h.Files.Where(f => !f.Destination.Contains("overlays", System.StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
                for (int i = 0; i < NUM_OVERLAYS; i++)
                {
                    if (appliedHacks.Any(h => h.Files.Any(f => f.Destination.Contains($"main_{i:X4}", System.StringComparison.OrdinalIgnoreCase))))
                    {
                        File.WriteAllLines(Path.Combine(project.BaseDirectory, "src", "overlays", $"main_{i:X4}", "symbols.x"), appliedHacks.SelectMany(h => h.Files.Where(f => f.Destination.Contains($"main_{i:X4}", System.StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
                    }
                }

                // Build and insert ARM9 hacks
                if (appliedHacks.Any(h => h.Files.Any(f => !f.Destination.Contains("overlays", System.StringComparison.OrdinalIgnoreCase))))
                {
                    ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(project.BaseDirectory, "src", "arm9.bin")), 0x02000000);
                    ARM9AsmHack.Insert(Path.Combine(project.BaseDirectory, "src"), arm9, 0x02005ECC, config.UseDocker ? config.DevkitArmDockerTag : string.Empty,
                        (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                        (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data));
                    Lib.IO.WriteBinaryFile(Path.Combine("rom", "arm9.bin"), arm9.GetBytes(), project, log);
                }
                else
                {
                    // Overlay compilation relies on the presence of an arm9_newcode.x containing the symbols of the newly compiled ARM9 code
                    // If there is no ARM9 code, we'll need to provide an empty file
                    foreach (string overlayDirectory in Directory.GetDirectories(Path.Combine(project.BaseDirectory, "src", "overlays")))
                    {
                        File.WriteAllText(Path.Combine(overlayDirectory, "arm9_newcode.x"), string.Empty);
                    }
                }

                // Get the overlays
                List<Overlay> overlays = new();
                string originalOverlaysDir = Path.Combine(project.BaseDirectory, "original", "overlay");
                string romInfoPath = Path.Combine(project.BaseDirectory, "original", $"{project.Name}.xml");
                string newRomInfoPath = Path.Combine(project.BaseDirectory, "rom", $"{project.Name}.xml");
                foreach (string file in Directory.GetFiles(originalOverlaysDir))
                {
                    overlays.Add(new(file, romInfoPath));
                }

                // Patch the overlays
                string overlaySourceDir = Path.Combine(project.BaseDirectory, "src", "overlays");
                for (int i = 0; i < overlays.Count; i++)
                {
                    if (Directory.GetDirectories(overlaySourceDir).Contains(Path.Combine(overlaySourceDir, overlays[i].Name)))
                    {
                        // If the overlay directory is empty, we've reverted all the hacks in it and should clean it up
                        if (!Directory.GetFiles(Path.Combine(overlaySourceDir, overlays[i].Name, "source")).Any())
                        {
                            Directory.Delete(Path.Combine(overlaySourceDir, overlays[i].Name), true);
                        }
                        else
                        {
                            OverlayAsmHack.Insert(overlaySourceDir, overlays[i], newRomInfoPath, config.UseDocker ? config.DevkitArmDockerTag : string.Empty,
                                (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                                (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data));
                        }
                    }
                }

                // Save all the overlays in case we've reverted all hacks on one
                foreach (Overlay overlay in overlays)
                {
                    overlay.Save(Path.Combine(project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"));
                    File.Copy(Path.Combine(project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"),
                        Path.Combine(project.IterativeDirectory, "rom", "overlay", $"{overlay.Name}.bin"), true);
                    project.Settings.File.RomInfo.ARM9Ovt.First(o => o.Id == overlay.Id).RamSize = (uint)overlay.Length;
                }

                // We don't provide visible errors during the compilation of the hacks because it will deadlock the threads
                // So at the end, we should check if any of the hacks that were supposed to be applied are not applied,
                // and if there are some then we should let the user know.
                IEnumerable<string> failedHackNames = appliedHacks.Where(h => !h.Applied(project)).Select(h => h.Name);
                if (failedHackNames.Any())
                {
                    log.LogError($"Some hacks ({string.Join(", ", failedHackNames)}) failed to apply -- check logs to see why.");
                }

                Close();
            };

            StackLayout buttonsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Items =
                {
                    importButton,
                    cancelButton,
                    saveButton,
                },
            };

            Content = new TableLayout(
                new TableRow(
                    new Scrollable
                    {
                        Content = hacksLayout,
                    }
                    ),
                new TableRow(
                    buttonsLayout
                    )
                );
        }
    }
}
