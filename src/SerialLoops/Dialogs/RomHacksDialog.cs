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

                foreach (CheckBox checkBox in hacksLayout.Items.Select(s => s.Control).Cast<CheckBox>())
                {
                    AsmHack hack = config.Hacks.First(f => f.Name == checkBox.Text);
                    bool applied = hack.Applied(project);

                    if (applied && (!checkBox.Checked ?? false))
                    {
                        hack.Revert(project);
                    }
                    else
                    {
                        hack.Apply(project, config, log);
                    }
                }

                ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(project.BaseDirectory, "src", "arm9.bin")), 0x02000000);
                ARM9AsmHack.Insert(Path.Combine(project.BaseDirectory, "src"), arm9, 0x02005ECC,
                    (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                    (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data, lookForErrors: true));
                Lib.IO.WriteBinaryFile(Path.Combine("rom", "arm9.bin"), arm9.GetBytes(), project, log);

                List<Overlay> overlays = new();
                string overlaySourceDir = Path.Combine(project.BaseDirectory, "original", "overlay");
                string romInfoPath = Path.Combine(project.BaseDirectory, "rom", $"{project.Name}.xml");
                foreach (string file in Directory.GetFiles(overlaySourceDir))
                {
                    overlays.Add(new(file, romInfoPath));
                }

                foreach (Overlay overlay in overlays)
                {
                    if (Directory.GetDirectories(overlaySourceDir).Contains(Path.Combine(overlaySourceDir, overlay.Name)))
                    {
                        OverlayAsmHack.Insert(overlaySourceDir, overlay, romInfoPath,
                            (object sender, DataReceivedEventArgs e) => log.Log(e.Data),
                            (object sender, DataReceivedEventArgs e) => log.LogWarning(e.Data, lookForErrors: true));
                    }
                }

                Thread.Sleep(3000);

                foreach (Overlay overlay in overlays)
                {
                    overlay.Save(Path.Combine(project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"));
                    overlay.Save(Path.Combine(project.IterativeDirectory, "rom", "overlay", $"{overlay.Name}.bin"));
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
