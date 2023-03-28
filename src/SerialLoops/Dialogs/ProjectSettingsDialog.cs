using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Utility;
using SkiaSharp;
using System;

namespace SerialLoops.Dialogs
{
    public class ProjectSettingsDialog : Dialog
    {
        private readonly Project _project;
        private readonly ILogger _log;
        private ProjectSettings Settings => _project.Settings;

        private TextArea _nameBox;
        private SKBitmap _newIcon;

        public ProjectSettingsDialog(Project project, ILogger log)
        {
            _project = project;
            _log = log;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Project Settings";
            Padding = 10;
            MinimumSize = new(300, 250);

            Button applyButton = new() { Text = "Apply" };
            applyButton.Click += ApplyButton_OnClick;

            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, e) => { Close(); };

            Content = new TableLayout(
                new TableRow(
                    new GroupBox
                    {
                        Text = "Game Banner",
                        Padding = 10,
                        Content = new StackLayout
                        {
                            Padding = 10,
                            Spacing = 10,
                            Orientation = Orientation.Horizontal,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            Items = { GetIconEditor(), GetNameEditor() }
                        }
                    }
                ),
                new TableRow(
                    new StackLayout
                    {
                        Padding = 10,
                        Spacing = 5,
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Items = { applyButton, cancelButton }
                    }
                )
            );
        }

        private Container GetIconEditor()
        {
            Panel iconPanel = new()
            {
                Content = GetPreview(Settings.Icon)
            };
            Button replaceButton = new() { Text = "Replace..." };
            replaceButton.Click += (sender, e) =>
            {
                OpenFileDialog dialog = new()
                {
                    Title = "Select Icon",
                    Filters = { new FileFilter("Image Files", ".png", ".jpg", ".jpeg", ".bmp", ".gif") }
                };

                if (dialog.ShowDialog(this) == DialogResult.Ok)
                {
                    SKBitmap newIcon = SKBitmap.Decode(dialog.FileName);
                    _newIcon = new(32, 32);
                    newIcon.ScalePixels(_newIcon, SKFilterQuality.High);
                    iconPanel.Content = GetPreview(_newIcon);
                }
            };

            return new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items =
                {
                    iconPanel,
                    replaceButton
                }
            };
        }

        private static SKGuiImage GetPreview(SKBitmap icon)
        {
            SKBitmap preview = new(64, 64);
            icon.ScalePixels(preview, SKFilterQuality.None);
            return new SKGuiImage(preview);
        }

        private Container GetNameEditor()
        {
            _nameBox = new()
            {
                Text = Settings.Name,
                Size = new(190, 50),
                SpellCheck = false,
                AcceptsTab = false
            };

            return new StackLayout
            {
                Padding = 10,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items = { "Game Title", _nameBox }
            };
        }

        private void ApplyButton_OnClick(object sender, EventArgs e)
        {
            string text = _nameBox.Text;
            if (text.Length is < 1 or > 127)
            {
                MessageBox.Show("Please enter a game name for the banner, between 1 and 128 characters.", MessageBoxType.Warning);
                return;
            }

            if (text.Split('\n').Length > 3)
            {
                MessageBox.Show("Game banner can only contain up to three lines.", MessageBoxType.Error);
                return;
            }
            Settings.Name = text;

            if (_newIcon is not null)
            {
                Settings.Icon = _newIcon;
            }

            _log.Log("Updated NDS Project File settings");
            Close();
        }

    }
}
