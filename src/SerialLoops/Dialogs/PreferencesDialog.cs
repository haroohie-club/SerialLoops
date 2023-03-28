using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Utility;
using System;

namespace SerialLoops.Dialogs
{
    public partial class PreferencesDialog : Dialog
    {
        public Config Configuration { get; set; }
        private ILogger _log { get; set; }

        private TextBox _devkitArmBox;
        private TextBox _emulatorBox;
        private CheckBox _autoReopenLastProjectBox;
        private CheckBox _rememberProjectWorkspaceBox;

        public PreferencesDialog(Config config, ILogger log)
        {
            Title = "Preferences";
            MinimumSize = new Size(300, 300);
            Configuration = config;
            _log = log;

            _devkitArmBox = new() { Text = Configuration.DevkitArmPath };
            _devkitArmBox.TextChanged += DevkitArmBox_TextChanged;

            _emulatorBox = new() { Text = Configuration.EmulatorPath };
            _emulatorBox.TextChanged += EmulatorBox_TextChanged;

            _autoReopenLastProjectBox = new() { Checked = Configuration.AutoReopenLastProject };
            _autoReopenLastProjectBox.CheckedChanged += AutoReopenLastProjectBox_CheckedChanged;

            _rememberProjectWorkspaceBox = new() { Checked = Configuration.RememberProjectWorkspace };
            _rememberProjectWorkspaceBox.CheckedChanged += RememberProjectWorkspaceBox_CheckedChanged;

            Button devkitArmButton = new() { Text = "Select" };
            devkitArmButton.Click += DevkitArmButton_Click;

            Button emulatorButton = new() { Text = "Select" };
            emulatorButton.Click += EmulatorButton_Click;

            Button saveButton = new() { Text = "Save" };
            saveButton.Click += SaveButton_Click;

            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, args) => Close();

            Content = new TableLayout(
                new TableRow(
                    new StackLayout
                    {
                        Padding = 10,
                        Spacing = 10,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            new GroupBox
                            {
                                Text = "Build",
                                Padding = 5,
                                Content = new StackLayout
                                {
                                    Spacing = 10,
                                    Orientation = Orientation.Vertical,
                                    Width = 275,
                                    Items =
                                    {
                                        ControlGenerator.GetControlWithLabel("DevkitARM Path",
                                            new StackLayout
                                            {
                                                Orientation = Orientation.Horizontal,
                                                Spacing = 5,
                                                Items =
                                                {
                                                    _devkitArmBox,
                                                    devkitArmButton,
                                                }
                                            }),
                                        ControlGenerator.GetControlWithLabel("Emulator Path",
                                            new StackLayout
                                            {
                                                Orientation = Orientation.Horizontal,
                                                Spacing = 5,
                                                Items =
                                                {
                                                    _emulatorBox,
                                                    emulatorButton,
                                                }
                                            }),
                                    }
                                },
                            },
                            new GroupBox
                            {
                                Text = "Projects",
                                Padding = 5,
                                Content = new StackLayout
                                {
                                    Spacing = 10,
                                    Orientation = Orientation.Vertical,
                                    Width = 275,
                                    Items =
                                    {
                                        ControlGenerator.GetControlWithLabel("Auto Re-open Last Project",
                                                                                   _autoReopenLastProjectBox),
                                        ControlGenerator.GetControlWithLabel("Remember Project Workspace",
                                                                                   _rememberProjectWorkspaceBox),
                                    }
                                },
                            }
                        }
                    }),
                new TableRow(
                    new StackLayout
                    {
                        Padding = 10,
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Items =
                        {
                            saveButton,
                            cancelButton
                        }
                    })
                );
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Configuration.Save(_log);
            Close();
        }

        private void DevkitArmBox_TextChanged(object sender, EventArgs e)
        {
            Configuration.DevkitArmPath = _devkitArmBox.Text;
        }

        private void DevkitArmButton_Click(object sender, EventArgs e)
        {
            SelectFolderDialog selectFolderDialog = new();
            if (selectFolderDialog.ShowAndReportIfFileSelected(this))
            {
                _devkitArmBox.Text = selectFolderDialog.Directory;
            }
        }

        private void EmulatorBox_TextChanged(object sender, EventArgs e)
        {
            Configuration.EmulatorPath = _emulatorBox.Text;
        }

        private void EmulatorButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                _emulatorBox.Text = openFileDialog.FileName;
            }
        }

        private void AutoReopenLastProjectBox_CheckedChanged(object sender, EventArgs e)
        {
            Configuration.AutoReopenLastProject = _autoReopenLastProjectBox.Checked ?? false;
        }

        private void RememberProjectWorkspaceBox_CheckedChanged(object sender, EventArgs e)
        {
            Configuration.RememberProjectWorkspace = _rememberProjectWorkspaceBox.Checked ?? false;
        }
    }
}
