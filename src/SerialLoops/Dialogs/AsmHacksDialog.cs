﻿using Eto.Forms;
using HaroohieClub.NitroPacker.Patcher.Nitro;
using HaroohieClub.NitroPacker.Patcher.Overlay;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SerialLoops.Dialogs
{
    public class AsmHacksDialog : Dialog
    {
        private const int NUM_OVERLAYS = 26;

        public AsmHacksDialog(Project project, Config config, ILogger log)
        {
            Title = "Apply Assembly Hacks";
            Padding = 5;


            StackLayout hacksLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Padding = 3,
                Size = new(200, 400),
            };

            StackLayout descriptionLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Padding = 3,
                Size = new(300, 400),
            };

            foreach (AsmHack hack in config.Hacks)
            {
                CheckBox hackCheckBox = new() { Text = hack.Name, Checked = hack.Applied(project) };
                hackCheckBox.MouseEnter += (sender, args) =>
                {
                    descriptionLayout.Items.Clear();
                    descriptionLayout.Items.Add(ControlGenerator.GetTextHeader(hack.Name));
                    descriptionLayout.Items.Add(new Label { Text = hack.Description, Wrap = WrapMode.Word });
                };
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
                if (appliedHacks.Any(h => h.Files.Any(f => !f.Destination.Contains("overlays", StringComparison.OrdinalIgnoreCase))))
                {
                    string arm9Path = Path.Combine(project.BaseDirectory, "src", "arm9.bin");
                    ARM9 arm9 = null;
                    try
                    {
                        arm9 = new(File.ReadAllBytes(arm9Path), 0x02000000);
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Failed to read ARM9 from '{arm9Path}'");
                        log.LogWarning($"{ex.Message}\n\n{ex.StackTrace}");
                    }

                    try
                    {
                        ARM9AsmHack.Insert(Path.Combine(project.BaseDirectory, "src"), arm9, 0x02005ECC, config.UseDocker ? config.DevkitArmDockerTag : string.Empty,
                            (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                            (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data),
                            devkitArmPath: config.DevkitArmPath);
                    }
                    catch (Exception ex)
                    {
                        log.LogError("Failed to insert ARM9 assembly hacks");
                        log.LogWarning($"{ex.Message}\n\n{ex.StackTrace}");
                    }

                    try
                    {
                        Lib.IO.WriteBinaryFile(Path.Combine("rom", "arm9.bin"), arm9.GetBytes(), project, log);
                    }
                    catch (Exception ex)
                    {
                        log.LogError("Failed to write ARM9 to disk");
                        log.LogWarning($"{ex.Message}\n\n{ex.StackTrace}");
                    }
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
                            try
                            {
                                OverlayAsmHack.Insert(overlaySourceDir, overlays[i], newRomInfoPath, config.UseDocker ? config.DevkitArmDockerTag : string.Empty,
                                    (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                                    (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data),
                                    devkitArmPath: config.DevkitArmPath);
                            }
                            catch (Exception ex)
                            {
                                log.LogError($"Failed to insert hacks into overlay {overlays[i].Name}");
                                log.LogWarning($"{ex.Message}\n\n{ex.StackTrace}");
                            }
                        }
                    }
                }

                // Save all the overlays in case we've reverted all hacks on one
                foreach (Overlay overlay in overlays)
                {
                    try
                    {
                        overlay.Save(Path.Combine(project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"));
                        File.Copy(Path.Combine(project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"),
                            Path.Combine(project.IterativeDirectory, "rom", "overlay", $"{overlay.Name}.bin"), true);
                        project.Settings.File.RomInfo.ARM9Ovt.First(o => o.Id == overlay.Id).RamSize = (uint)overlay.Length;
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Failed saving overlay {overlay.Name} to disk");
                        log.LogWarning($"{ex.Message}\n\n{ex.StackTrace}");
                    }
                }

                // We don't provide visible errors during the compilation of the hacks because it will deadlock the threads
                // So at the end, we should check if any of the hacks that were supposed to be applied are not applied,
                // and if there are some then we should let the user know.
                IEnumerable<string> failedHackNames = appliedHacks.Where(h => !h.Applied(project)).Select(h => h.Name);
                if (failedHackNames.Any())
                {
                    log.LogError($"Failed to apply the following hacks to the ROM:\n{string.Join(", ", failedHackNames)}\n\nPlease check the log file for more information.");
                }
                else
                {
                    if (appliedHacks.Any())
                    {
                        MessageBox.Show($"Successfully applied the following hacks:\n{string.Join(", ", appliedHacks.Select(h => h.Name))}", "Successfully applied hacks!", MessageBoxType.Information);
                    }
                    else
                    {
                        MessageBox.Show("No hacks applied!", "Success!", MessageBoxType.Information);
                    }
                }

                Close();
            };

            StackLayout buttonsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Padding = 5,
                Items =
                {
                    importButton,
                    saveButton,
                    cancelButton,
                },
            };

            Content = new TableLayout(
                new TableRow(
                    new Scrollable
                    {
                        Content = hacksLayout,
                    },
                    descriptionLayout
                    ),
                new TableRow(
                    buttonsLayout
                    )
                )
            {
                Spacing = new(10, 0),
            };
        }
    }
}
