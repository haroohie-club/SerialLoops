using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Utility;
using System;
using System.Globalization;

namespace SerialLoops.Dialogs
{
    public partial class PreferencesDialog : Dialog
    {
        public Config Configuration { get; set; }
        private ILogger _log { get; set; }
        public bool RequireRestart { get; set; } = false;


        public PreferencesDialog(Config config, ILogger log)
        {
            Title = Application.Instance.Localize(this, "Preferences");
            MinimumSize = new Size(550, 600);
            Size = new Size(550, 600);
            Resizable = true;
            Configuration = config;
            _log = log;

            Button saveButton = new() { Text = Application.Instance.Localize(this, "Save") };
            saveButton.Click += SaveButton_Click;

            Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
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
                        Application.Instance.Localize(this, "Build"),
                        [
                            new FolderOption
                            {
                                Name = Application.Instance.Localize(this, "devkitARM Path"),
                                Path = Configuration.DevkitArmPath,
                                OnChange = (path) => Configuration.DevkitArmPath = path
                            },
                            new FileOption
                            {
                                Name = Application.Instance.Localize(this, "Emulator Path"),
                                Path = Configuration.EmulatorPath,
                                OnChange = (path) => Configuration.EmulatorPath = path
                            },
                            new BooleanOption
                            {
                                Name = Application.Instance.Localize(this, "Use Docker for ASM Hacks"),
                                Value = Configuration.UseDocker,
                                OnChange = (value) => Configuration.UseDocker = value,
                                Enabled = !Platform.IsMac,
                            },
                            new TextOption
                            {
                                Name = Application.Instance.Localize(this, "devkitARM Docker Tag"),
                                Value = Configuration.DevkitArmDockerTag,
                                OnChange = (value) => Configuration.DevkitArmDockerTag = value,
                                Enabled = !Platform.IsMac,
                            },
                        ]
                    ),
                    new OptionsGroup(
                        Application.Instance.Localize(this, "Projects"),
                        [
                            new BooleanOption
                            {
                                Name = Application.Instance.Localize(this, "Auto Re-Open Last Project"),
                                Value = Configuration.AutoReopenLastProject,
                                OnChange = (value) => Configuration.AutoReopenLastProject = value
                            },
                            new BooleanOption
                            {
                                Name = Application.Instance.Localize(this, "Remember Project Workspace"),
                                Value = Configuration.RememberProjectWorkspace,
                                OnChange = (value) => Configuration.RememberProjectWorkspace = value
                            },
                            new BooleanOption
                            {
                                Name = Application.Instance.Localize(this, "Remove Missing Projects"),
                                Value = Configuration.RemoveMissingProjects,
                                OnChange = (value) => Configuration.RemoveMissingProjects = value
                            }
                        ]
                    ),
                    new OptionsGroup(
                        "Serial Loops",
                        [
                            new DropDownOption(
                                [
                                    ("en-US", "English (United States)"),
                                    ("de", "Deutsch"),
                                    ("en-GB", "English (United Kingdom)"),
                                    ("it", "Italiano"),
                                    ("ja", "日本語"),
                                    ("pt-BR", "Português brasileiro"),
                                    ("zh-Hans", "中文（简化字）"),
                                ])
                            {
                                Name = Application.Instance.Localize(this, "Language"),
                                Value = CultureInfo.CurrentCulture.Name,
                                OnChange = (value) =>
                                {
                                    CultureInfo.CurrentCulture = new(value);
                                    Configuration.CurrentCultureName = value;
                                    RequireRestart = true;
                                }
                            },
                            new BooleanOption
                            {
                                Name = Application.Instance.Localize(this, "Check for Updates on Startup"),
                                Value = Configuration.CheckForUpdates,
                                OnChange = (value) => Configuration.CheckForUpdates = value
                            },
                            new BooleanOption
                            {
                                Name = Application.Instance.Localize(this, "Use Pre-Release Update Channel"),
                                Value = Configuration.PreReleaseChannel,
                                OnChange = (value) => Configuration.PreReleaseChannel = value
                            }
                        ]
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
