using Eto.Forms;
using SerialLoops.Lib;
using SerialLoops.Utility;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace SerialLoops.Dialogs
{
    public class ProjectImportDialog : Dialog<(string Slzip, string Rom)>
    {
        private string _romHash;
        private string _slzipPath = string.Empty;
        private string _romPath = string.Empty;

        public ProjectImportDialog()
        {
            _romHash = Application.Instance.Localize(this, "Select an exported project to see expected ROM hash");
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Label expectedRomHash = new() { Text = string.Format(Application.Instance.Localize(this, "Expected ROM CRC32 Hash: {0}"), _romHash) };
            Label baseRomPath = new() { Text = Application.Instance.Localize(this, ProjectCreationDialog.NO_ROM_TEXT) };
            Label slzipPath = new() { Text = Application.Instance.Localize(this, "No exported project selected") };
            CheckBox overrideHashCheckBox = new() { Text = "Ignore Hash?", Enabled = false, Checked = false };

            Button slzipButton = new() { Text = Application.Instance.Localize(this, "Open Exported Project") };
            slzipButton.Click += (sender, args) =>
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Filters.Add(new(Application.Instance.Localize(this, "Serial Loops Exported Project"), $".{Project.EXPORT_FORMAT}"));
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    _slzipPath = openFileDialog.FileName;
                    slzipPath.Text = _slzipPath;
                    using FileStream slzipFs = File.OpenRead(_slzipPath);
                    using ZipArchive slzip = new(slzipFs);
                    _romHash = slzip.Comment;
                    expectedRomHash.Text = string.Format(Application.Instance.Localize(this, "Expected ROM SHA-1 Hash: {0}"), $"{_romHash:X8}");
                }
            };

            Button baseRomButton = new() { Text = Application.Instance.Localize(this, "Open ROM") };
            baseRomButton.Click += (sender, args) =>
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Filters.Add(new(Application.Instance.Localize(this, "Chokuretsu ROM"), ".nds"));
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    byte[] romBytes = File.ReadAllBytes(openFileDialog.FileName);
                    string expectedHash = string.Join("", SHA1.HashData(romBytes).Select(b => $"{b:X2}"));
                    if (!_romHash.Equals(expectedHash))
                    {
                        MessageBox.Show(this,
                            Application.Instance.Localize(this, "The selected ROM's hash does not match the expected ROM hash. Please ensure you are using the correct base ROM.\n\nIf you wish to ignore this, please check the \"Ignore Hash\" checkbox."),
                            Application.Instance.Localize(this, "ROM Hash Mismatch"), MessageBoxType.Warning);
                        overrideHashCheckBox.Enabled = true;
                    }
                    else
                    {
                        overrideHashCheckBox.Checked = false;
                        overrideHashCheckBox.Enabled = false;
                    }
                    _romPath = openFileDialog.FileName;
                    baseRomPath.Text = _romPath;
                }
            };

            Button importButton = new() { Text = Application.Instance.Localize(this, "Import") };
            importButton.Click += (sender, args) =>
            {
                if (string.IsNullOrEmpty(_slzipPath) || string.IsNullOrEmpty(_romPath))
                {
                    MessageBox.Show(this, Application.Instance.Localize(this, "Both an exported project and a base ROM must be selected to import a project."), Application.Instance.Localize(this, "Error"), MessageBoxType.Error);
                }
                else if (overrideHashCheckBox.Enabled && !(overrideHashCheckBox.Checked ?? false))
                {
                    MessageBox.Show(this, Application.Instance.Localize(this, "The base ROM hash does not match the expected hash! Please check the \"Ignore Hash\" checkbox if you wish to override this."),
                        Application.Instance.Localize(this, "Error"), MessageBoxType.Error);
                }
                else
                {
                    Close((_slzipPath, _romPath));
                }
            };

            Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
            cancelButton.Click += (sender, args) => Close();

            Content = new StackLayout()
            {
                Spacing = 10,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Orientation = Orientation.Vertical,
                Items =
                {
                    expectedRomHash,
                    new StackLayout
                    {
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        Items =
                        {
                            slzipButton,
                            slzipPath,
                        }
                    },
                    new StackLayout
                    {
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        Items =
                        {
                            baseRomButton,
                            baseRomPath,
                        }
                    },
                    overrideHashCheckBox,
                    new StackLayout
                    {
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        Items =
                        {
                            importButton,
                            cancelButton,
                        }
                    }
                }
            };
        }
    }
}
