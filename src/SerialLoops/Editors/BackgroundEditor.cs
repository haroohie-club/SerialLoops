using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.IO;

namespace SerialLoops.Editors
{
    public class BackgroundEditor : Editor
    {
        private BackgroundItem _bg;

        public BackgroundEditor(BackgroundItem item, ILogger log) : base(item, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _bg = (BackgroundItem)Description;
            StackLayout extrasInfo = new();

            Button exportButton = new() { Text = "Export" };
            exportButton.Click += ExportButton_Click;

            Button replaceButton = new() { Text = "Replace" };
            replaceButton.Click += ReplaceButton_Click;

            if (!string.IsNullOrEmpty(_bg.CgName))
            {
                extrasInfo.Items.Add(_bg.CgName);
                extrasInfo.Items.Add($"Unknown Extras Short: {_bg.ExtrasShort}");
                extrasInfo.Items.Add($"Unknown Extras Integer: {_bg.ExtrasInt}");
            }

            return new Scrollable
            {
                Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 5,
                    Items =
                    {
                        new ImageView() { Image = new SKGuiImage(_bg.GetBackground()) },
                        $"{_bg.Id} (0x{_bg.Id:X3}); {_bg.BackgroundType}",
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 3,
                            Items =
                            {
                                exportButton,
                                replaceButton,
                            }
                        },
                        extrasInfo,
                    }
                }
            };
        }

        private void ExportButton_Click(object sender, System.EventArgs e)
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filters.Add(new() { Name = "PNG Image", Extensions = new string[] { ".png" } });
            if (saveFileDialog.ShowAndReportIfFileSelected(this))
            {
                try
                {
                    using FileStream fs = File.Create(saveFileDialog.FileName);
                    _bg.GetBackground().Encode(fs, SkiaSharp.SKEncodedImageFormat.Png, 1);
                }
                catch (Exception ex)
                {
                    _log.LogError($"Failed to export background {_bg.DisplayName} to file {saveFileDialog.FileName}: {ex.Message}\n\n{ex.StackTrace}");
                }
            }
        }

        private void ReplaceButton_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filters.Add(new() { Name = "PNG Image", Extensions = new string[] { ".png" } });
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                try
                {
                    LoopyProgressTracker tracker = new();
                    _ = new ProgressDialog(() => _bg.SetBackground(SKBitmap.Decode(openFileDialog.FileName), tracker),
                        () => Content = GetEditorPanel(), tracker, $"Replacing {_bg.DisplayName}...");
                    UpdateTabTitle(false);
                }
                catch (Exception ex)
                {
                    _log.LogError($"Failed to replace background {_bg.DisplayName} with file {openFileDialog.FileName}: {ex.Message}\n\n{ex.StackTrace}");
                }
            }
        }
    }
}
