using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Utility;
using SkiaSharp;
using System;

namespace SerialLoops
{
    public partial class ProjectSettingsDialog : Dialog
    {
        private Project _project;
        private ProjectSettings _settings { get => _project.Settings; }

        private TextBox _nameBox;
        private TextBox _authorBox;

        private SKBitmap _newIcon;

        private readonly ILogger _log;

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
                Content = GetPreview(_settings.Icon)
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
                    _newIcon = SKBitmap.Decode(dialog.FileName);
                    _settings.Icon = _newIcon; // todo
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
            string[] names = _settings.Name.Split("\n");
            _nameBox = new()
            {
                Text = names[0],
                PlaceholderText = "Name",
                Size = new(175, 23),
                MaxLength = 64,
            };
            _authorBox = new()
            {
                Text = names[1],
                PlaceholderText = "Author",
                Size = new(175, 23),
                MaxLength = 64
            };

            return new StackLayout
            {
                Padding = 10,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items =
                {
                    _nameBox,
                    _authorBox
                }
            };
        }

        private void ApplyButton_OnClick(object sender, EventArgs e)
        {
            string text = $"{_nameBox.Text}\n{_authorBox.Text}";
            if (_nameBox.Text.Length < 1 || _authorBox.Text.Length < 1)
            {
                MessageBox.Show("Please enter a name and author for the DS Menu banner", MessageBoxType.Error);
                return;
            }
            _settings.Name = text;

            if (_newIcon != null)
            {
                _settings.Icon = _newIcon;
            }
            _log.Log("Updated NDS Project File settings");
            Close();
        }

    }
}
