using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using System;
using SerialLoops.Utility;

namespace SerialLoops.Dialogs
{
    public partial class PreferencesDialog : Dialog
    {
        public Config Configuration { get; set; }
        private ILogger _log { get; set; }

        public PreferencesDialog(Config config, ILogger log)
        {
            Title = "Preferences";
            MinimumSize = new Size(700, 600);
            Size = new Size(700, 600);
            Resizable = true;
            Configuration = config;
            _log = log;

            Button saveButton = new() { Text = "Save" };
            saveButton.Click += SaveButton_Click;

            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, args) => Close();

            Content = new TableLayout(
                new TableRow(InitializeOptions()),
                new TableRow(
                    new StackLayout
                    {
                        Padding = 10,
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment = VerticalAlignment.Bottom,
                        Items =
                        {
                            saveButton,
                            cancelButton
                        }
                    })
                );
        }

        private StackLayout InitializeOptions()
        {
            return new()
            {
                Padding = 10,
                Spacing = 10,
                Orientation = Orientation.Vertical,
                Items =
                {
                    new OptionsGroup
                    (
                        "Build",
                        new()
                        {
                            new FolderOption
                            {
                                Name = "DevkitARM Path",
                                Path = Configuration.DevkitArmPath,
                                OnChange = (path) => Configuration.DevkitArmPath = path
                            },
                            new FileOption
                            {
                                Name = "Emulator Path",
                                Path = Configuration.EmulatorPath,
                                OnChange = (path) => Configuration.EmulatorPath = path
                            },
                            new BooleanOption
                            {
                                Name = "Use Docker for ASM Hacks",
                                Value = Configuration.UseDocker,
                                OnChange = (value) => Configuration.UseDocker = value
                            },
                            new TextOption
                            {
                                Name = "DevkitARM Docker Tag (for use with ASM Hacks)",
                                Value = Configuration.DevkitArmDockerTag,
                                OnChange = (value) => Configuration.DevkitArmDockerTag = value
                            },
                        }
                    ),
                    new OptionsGroup(
                        "Projects",
                        new()
                        {
                            new BooleanOption
                            {
                                Name = "Auto Re-Open Last Project",
                                Value = Configuration.AutoReopenLastProject,
                                OnChange = (value) => Configuration.AutoReopenLastProject = value
                            },
                            new BooleanOption
                            {
                                Name = "Remember Project Workspace",
                                Value = Configuration.RememberProjectWorkspace,
                                OnChange = (value) => Configuration.RememberProjectWorkspace = value
                            },
                            new BooleanOption
                            {
                                Name = "Remove Missing Projects",
                                Value = Configuration.RemoveMissingProjects,
                                OnChange = (value) => Configuration.RemoveMissingProjects = value
                            }
                        }
                    ),
                    new OptionsGroup(
                        "Serial Loops",
                        new()
                        {
                            new BooleanOption
                            {
                                Name = "Check For Updates On Startup",
                                Value = Configuration.CheckForUpdates,
                                OnChange = (value) => Configuration.CheckForUpdates = value
                            },
                            new BooleanOption
                            {
                                Name = "Use Pre-Release Update Channel",
                                Value = Configuration.PreReleaseChannel,
                                OnChange = (value) => Configuration.PreReleaseChannel = value
                            }
                        }
                    )
                }
            };
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Configuration.Save(_log);
            Close();
        }
    }
}
