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
using System.Threading;

namespace SerialLoops.Dialogs
{
    public class RomHacksDialog : Dialog
    {
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
                LoopyProgressTracker tracker = new();

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

                File.WriteAllLines(Path.Combine(project.BaseDirectory, "src", "symbols.x"), appliedHacks.SelectMany(h => h.Files.Where(f => !f.Destination.Contains("overlays", System.StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
                for (int i = 0; i < 26; i++)
                {
                    if (appliedHacks.Any(h => h.Files.Any(f => f.Destination.Contains($"main_{i:X4}", System.StringComparison.OrdinalIgnoreCase))))
                    {
                        File.WriteAllLines(Path.Combine(project.BaseDirectory, "src", "overlays", $"main_{i:X4}", "symbols.x"), appliedHacks.SelectMany(h => h.Files.Where(f => f.Destination.Contains($"main_{i:X4}", System.StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
                    }
                }

                if (appliedHacks.Any(h => h.Files.Any(f => !f.Destination.Contains("overlays", System.StringComparison.OrdinalIgnoreCase))))
                {
                    ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(project.BaseDirectory, "src", "arm9.bin")), 0x02000000);
                    ARM9AsmHack.Insert(Path.Combine(project.BaseDirectory, "src"), arm9, 0x02005ECC,
                        (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                        (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data, lookForErrors: true));
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

                List<Overlay> overlays = new();
                string originalOverlaysDir = Path.Combine(project.BaseDirectory, "original", "overlay");
                string romInfoPath = Path.Combine(project.BaseDirectory, "original", $"{project.Name}.xml");
                string newRomInfoPath = Path.Combine(project.BaseDirectory, "rom", $"{project.Name}.xml");
                foreach (string file in Directory.GetFiles(originalOverlaysDir))
                {
                    overlays.Add(new(file, newRomInfoPath));
                }

                string overlaySourceDir = Path.Combine(project.BaseDirectory, "src", "overlays");
                for (int i = 0; i < overlays.Count; i++)
                {
                    if (Directory.GetDirectories(overlaySourceDir).Contains(Path.Combine(overlaySourceDir, overlays[i].Name)))
                    {
                        OverlayAsmHack.Insert(overlaySourceDir, overlays[i], newRomInfoPath,
                            (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                            (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data, lookForErrors: true));
                    }
                    else
                    {
                        overlays.RemoveAt(i);
                        i--;
                    }
                }

                foreach (Overlay overlay in overlays)
                {
                    bool baseSuccess = false, iterativeSuccess = false;
                    while (!baseSuccess || !iterativeSuccess)
                    {
                        try
                        {
                            if (!baseSuccess)
                            {
                                overlay.Save(Path.Combine(project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"));
                                baseSuccess = true;
                            }
                            if (!iterativeSuccess)
                            {
                                overlay.Save(Path.Combine(project.IterativeDirectory, "rom", "overlay", $"{overlay.Name}.bin"));
                                iterativeSuccess = true;
                            }
                        }
                        catch (IOException)
                        {
                            continue;
                        }
                    }
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
