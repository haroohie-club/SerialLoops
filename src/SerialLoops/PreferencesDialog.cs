using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Utility;

namespace SerialLoops
{
    public partial class PreferencesDialog : Dialog
    {
        public Config Configuration { get; set; }
        private ILogger _log { get; set; }

        private TextBox _devkitArmBox;
        private TextBox _emulatorBox;

        public PreferencesDialog(Config config, ILogger log)
        {
            Title = "Preferences";
            MinimumSize = new Size(200, 200);
            Configuration = config;
            _log = log;

            _devkitArmBox = new() { Text = Configuration.DevkitArmPath };
            _devkitArmBox.TextChanged += DevkitArmBox_TextChanged;
            
            _emulatorBox = new() { Text = Configuration.EmulatorPath };
            _emulatorBox.TextChanged += EmulatorBox_TextChanged;

            Button devkitArmButton = new() { Text = "Select" };
            devkitArmButton.Click += DevkitArmButton_Click;
            
            Button emulatorButton = new() { Text = "Select" };
            emulatorButton.Click += EmulatorButton_Click;

            Button saveButton = new() { Text = "Save Preferences" };
            saveButton.Click += SaveButton_Click;

            Content = new TableLayout(
                new TableRow(
                    new GroupBox
                    {
                        Text = "Paths",
                        Padding = 5,
                        Content = new StackLayout
                        {
                            Spacing = 10,
                            Orientation = Orientation.Vertical,
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
                    }),
                new TableRow(saveButton)
                );
        }

        private void SaveButton_Click(object sender, System.EventArgs e)
        {
            Configuration.Save(_log);
            Close();
        }

        private void DevkitArmBox_TextChanged(object sender, System.EventArgs e)
        {
            Configuration.DevkitArmPath = _devkitArmBox.Text;
        }

        private void DevkitArmButton_Click(object sender, System.EventArgs e)
        {
            SelectFolderDialog selectFolderDialog = new();
            if (selectFolderDialog.ShowAndReportIfFileSelected(this))
            {
                _devkitArmBox.Text = selectFolderDialog.Directory;
            }
        }
        
        private void EmulatorBox_TextChanged(object sender, System.EventArgs e)
        {
            Configuration.EmulatorPath = _emulatorBox.Text;
        }

        private void EmulatorButton_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                _emulatorBox.Text = openFileDialog.FileName;
            }
        }
    }
}
