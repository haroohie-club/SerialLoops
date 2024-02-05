using Eto.Forms;
using HaroohieClub.NitroPacker.Patcher.Nitro;
using HaroohieClub.NitroPacker.Patcher.Overlay;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Lib.Util;
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

        private Dictionary<HackFile, SelectedHackParameter[]> _selectedHackParameters = [];
        private AsmHack _currentHack;

        public AsmHacksDialog(Project project, Config config, ILogger log)
        {
            Title = Application.Instance.Localize(this, "Apply Assembly Hacks");
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
                hack.ValueChanged = false;
                foreach (HackFile file in hack.Files)
                {
                    try
                    {
                        _selectedHackParameters.Add(file, file.Parameters.Select(p => new SelectedHackParameter { Parameter = p, Selection = 0 }).ToArray());
                    }
                    catch (Exception ex)
                    {
                        log.LogException(string.Format(Application.Instance.Localize(this, "Failed to add parameters for hack file {0} in hack {1}"), file.File, hack.Name), ex);
                    }
                }

                CheckBox hackCheckBox = new() { Text = hack.Name, Checked = hack.Applied(project) };
                hackCheckBox.MouseMove += (sender, args) =>
                {
                    if (hack == _currentHack)
                    {
                        return;
                    }

                    _currentHack = hack;
                    descriptionLayout.Items.Clear();
                    descriptionLayout.Items.Add(ControlGenerator.GetTextHeader(hack.Name));
                    descriptionLayout.Items.Add(new Label { Text = hack.Description, Wrap = WrapMode.Word });
                    StackLayout parametersLayout = new()
                    {
                        Spacing = 5,
                        Padding = 3,
                    };
                    GroupBox parametersBox = new()
                    {
                        Text = "Parameters",
                        Content = parametersLayout,
                    };
                    foreach (HackFile file in hack.Files)
                    {
                        for (int i = 0; i < _selectedHackParameters[file].Length; i++)
                        {
                            int currentParam = i; // need this as i increments and will mess up the SelectedIndexChanged method
                            ComboBox parameterListBox = new();
                            parameterListBox.Items.AddRange(_selectedHackParameters[file][currentParam].Parameter.Values.Select(v => new ListItem { Key = v.Name, Tag = file, Text = v.Name }));
                            parameterListBox.SelectedIndex = _selectedHackParameters[file][currentParam].Selection;
                            parameterListBox.SelectedIndexChanged += (sender, args) =>
                            {
                                hack.ValueChanged = true;
                                _selectedHackParameters[file][currentParam].Selection = parameterListBox.SelectedIndex;
                            };
                            parametersLayout.Items.Add(ControlGenerator.GetControlWithLabel(_selectedHackParameters[file][currentParam].Parameter.DescriptiveName, parameterListBox));
                        }
                    }
                    descriptionLayout.Items.Add(parametersBox);
                };
                hacksLayout.Items.Add(hackCheckBox);
            }

            Button importButton = new()
            {
                Text = Application.Instance.Localize(this, "Import Hack"),
            };
            importButton.Click += (sender, args) =>
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Import a Hack")
                };
                openFileDialog.Filters.Add(new(Application.Instance.Localize(this, "Serialized ASM hack"), ".slhack"));

                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    AsmHack hack = JsonSerializer.Deserialize<AsmHack>(File.ReadAllText(openFileDialog.FileName));

                    if (config.Hacks.Any(h => h.Files.Any(f => hack.Files.Contains(f))))
                    {
                        log.LogError("Error: duplicate hack detected! A file with the same name as a file in this hack has already been imported.");
                        return;
                    }
                    else if (config.Hacks.Any(h => h.Name == hack.Name))
                    {
                        log.LogError("Error: duplicate hack detected! A hack with the same name has already been imported.");
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
                Text = Application.Instance.Localize(this, "Cancel"),
            };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            Button saveButton = new()
            {
                Text = Application.Instance.Localize(this, "Save"),
            };
            saveButton.Click += (sender, args) =>
            {
                List<AsmHack> appliedHacks = [];
                foreach (CheckBox checkBox in hacksLayout.Items.Select(s => s.Control).Cast<CheckBox>())
                {
                    AsmHack hack = config.Hacks.First(f => f.Name == checkBox.Text);
                    bool applied = hack.Applied(project);

                    if (applied && (!checkBox.Checked ?? false))
                    {
                        hack.Revert(project, log);
                    }
                    else if (applied && !hack.ValueChanged)
                    {
                        appliedHacks.Add(hack);
                    }
                    else if (checkBox.Checked ?? true)
                    {
                        hack.Apply(project, config, _selectedHackParameters.Where(kv => hack.Files.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value), log);
                        appliedHacks.Add(hack);
                    }
                }

                // Write the symbols file based on what the hacks say they need
                File.WriteAllLines(Path.Combine(project.BaseDirectory, "src", "symbols.x"), appliedHacks.SelectMany(h => h.Files.Where(f => !f.Destination.Contains("overlays", StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
                for (int i = 0; i < NUM_OVERLAYS; i++)
                {
                    if (appliedHacks.Any(h => h.Files.Any(f => f.Destination.Contains($"main_{i:X4}", StringComparison.OrdinalIgnoreCase))))
                    {
                        File.WriteAllLines(Path.Combine(project.BaseDirectory, "src", "overlays", $"main_{i:X4}", "symbols.x"), appliedHacks.SelectMany(h => h.Files.Where(f => f.Destination.Contains($"main_{i:X4}", StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
                    }
                }

                // Build and insert ARM9 hacks
                string arm9Path = Path.Combine(project.BaseDirectory, "src", "arm9.bin");
                ARM9 arm9 = null;
                try
                {
                    arm9 = new(File.ReadAllBytes(arm9Path), 0x02000000);
                }
                catch (Exception ex)
                {
                    log.LogException(string.Format(Application.Instance.Localize(this, "Failed to read ARM9 from '{0}'"), arm9Path), ex);
                }

                try
                {
                    LoopyProgressTracker tracker = new();
                    ProgressDialog _ = new(() =>
                    {
                        ARM9AsmHack.Insert(Path.Combine(project.BaseDirectory, "src"), arm9, 0x02005ECC, config.UseDocker ? config.DevkitArmDockerTag : string.Empty,
                            (object sender, DataReceivedEventArgs e) => { log.Log(e.Data); ((IProgressTracker)tracker).Focus(e.Data, 1); },
                            (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data),
                            devkitArmPath: config.DevkitArmPath);
                    }, () => {}, tracker, Application.Instance.Localize(this, "Patching ARM9"));
                }
                catch (Exception ex)
                {
                    log.LogException("Failed to insert ARM9 assembly hacks", ex);
                }

                try
                {
                    Lib.IO.WriteBinaryFile(Path.Combine("rom", "arm9.bin"), arm9.GetBytes(), project, log);
                }
                catch (Exception ex)
                {
                    log.LogException("Failed to write ARM9 to disk", ex);
                }
                if (appliedHacks.All(h => h.Files.Any(f => f.Destination.Contains("overlays", StringComparison.OrdinalIgnoreCase))))
                {
                    // Overlay compilation relies on the presence of an arm9_newcode.x containing the symbols of the newly compiled ARM9 code
                    // If there is no ARM9 code, we'll need to provide an empty file
                    foreach (string overlayDirectory in Directory.GetDirectories(Path.Combine(project.BaseDirectory, "src", "overlays")))
                    {
                        File.WriteAllText(Path.Combine(overlayDirectory, "arm9_newcode.x"), string.Empty);
                    }
                }

                // Get the overlays
                List<Overlay> overlays = [];
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
                        if (Directory.GetFiles(Path.Combine(overlaySourceDir, overlays[i].Name, "source")).Length == 0)
                        {
                            Directory.Delete(Path.Combine(overlaySourceDir, overlays[i].Name), true);
                        }
                        else
                        {
                            try
                            {
                                LoopyProgressTracker tracker = new();
                                ProgressDialog _ = new(() =>
                                {
                                    OverlayAsmHack.Insert(overlaySourceDir, overlays[i], newRomInfoPath, config.UseDocker ? config.DevkitArmDockerTag : string.Empty,
                                    (object sender, DataReceivedEventArgs e) => { log.Log(e.Data); ((IProgressTracker)tracker).Focus(e.Data, 1); },
                                    (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data),
                                    devkitArmPath: config.DevkitArmPath);
                                }, () => { }, tracker, string.Format(Application.Instance.Localize(this, "Patching Overlay {0}"), overlays[i].Name));
                            }
                            catch (Exception ex)
                            {
                                log.LogException(string.Format(Application.Instance.Localize(this, "Failed to insert hacks into overlay {0}"), overlays[i].Name), ex);
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
                        log.LogException(string.Format(Application.Instance.Localize(this, "Failed saving overlay {0} to disk"), overlay.Name), ex);
                    }
                }

                // We don't provide visible errors during the compilation of the hacks because it will deadlock the threads
                // So at the end, we should check if any of the hacks that were supposed to be applied are not applied,
                // and if there are some then we should let the user know.
                IEnumerable<string> failedHackNames = appliedHacks.Where(h => !h.Applied(project)).Select(h => h.Name);
                if (failedHackNames.Any())
                {
                    log.LogError(string.Format(Application.Instance.Localize("Failed to apply the following hacks to the ROM:\n{0}\n\nPlease check the log file for more information.\n\nIn order to preserve state, no hacks were applied.", string.Join(", ", failedHackNames))));
                    foreach (AsmHack hack in appliedHacks)
                    {
                        hack.Revert(project, log);
                    }
                    IEnumerable<string> dirsToDelete = Directory.GetDirectories(Path.Combine(project.BaseDirectory, "src"), "-p", SearchOption.AllDirectories)
                        .Concat(Directory.GetDirectories(Path.Combine(project.BaseDirectory, "src"), "build", SearchOption.AllDirectories));
                    foreach (string dir in dirsToDelete)
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                    string[] filesToDelete = Directory.GetFiles(Path.Combine(project.BaseDirectory, "src"), "arm9_newcode.x", SearchOption.AllDirectories);
                    foreach (string file in filesToDelete)
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    if (appliedHacks.Count != 0)
                    {
                        MessageBox.Show(string.Format(Application.Instance.Localize(this, "Successfully applied the following hacks:\n{0}"), string.Join(", ", appliedHacks.Select(h => h.Name))), Application.Instance.Localize(this, "Successfully applied hacks!"), MessageBoxType.Information);
                    }
                    else
                    {
                        MessageBox.Show(Application.Instance.Localize(this, "No hacks applied!"), Application.Instance.Localize(this, "Success!"), MessageBoxType.Information);
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
