using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;

namespace SerialLoops.Editors
{
    public class BackgroundEditor(BackgroundItem item, Project project, ILogger log) : Editor(item, log, project)
    {
        private BackgroundItem _bg;

        public override Container GetEditorPanel()
        {
            _bg = (BackgroundItem)Description;
            StackLayout extrasInfo = new();

            Button exportButton = new() { Text = Application.Instance.Localize(this, "Export") };
            exportButton.Click += ExportButton_Click;

            Button replaceButton = new() { Text = Application.Instance.Localize(this, "Replace") };
            replaceButton.Click += ReplaceButton_Click;

            if (!string.IsNullOrEmpty(_bg.CgName))
            {
                TextBox cgNameBox = new() { Text = _bg.CgName, Width = 200 };
                cgNameBox.TextChanged += (sender, args) =>
                {
                    _project.Extra.Cgs[_project.Extra.Cgs.IndexOf(_project.Extra.Cgs.First(b => b.Name.GetSubstitutedString(_project).Equals(_bg.CgName)))].Name = cgNameBox.Text.GetOriginalString(_project);
                    _bg.CgName = cgNameBox.Text;
                    UpdateTabTitle(false, cgNameBox);
                };

                extrasInfo.Items.Add(cgNameBox);
                extrasInfo.Items.Add(string.Format(Application.Instance.Localize(this, "Flag: {0}"), _bg.Flag));
                extrasInfo.Items.Add(string.Format(Application.Instance.Localize(this, "Unknown Extras Short: {0}"), _bg.ExtrasShort));
                extrasInfo.Items.Add(string.Format(Application.Instance.Localize(this, "Unknown Extras Byte: {0}"), _bg.ExtrasByte));
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

        private void ExportButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "PNG Image"), Extensions = [".png"] });
            if (saveFileDialog.ShowAndReportIfFileSelected(this))
            {
                try
                {
                    using FileStream fs = File.Create(saveFileDialog.FileName);
                    _bg.GetBackground().Encode(fs, SKEncodedImageFormat.Png, 1);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to export background {0} to file {1}"), _bg.DisplayName, saveFileDialog.FileName), ex);
                }
            }
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            SKBitmap original = _bg.GetBackground();
            openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "Supported Images"), Extensions = [".bmp", ".gif", ".heif", ".jpg", ".jpeg", ".png", ".webp",] });
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                ImageCropResizeDialog bgResizeDialog = new(SKBitmap.Decode(openFileDialog.FileName), original.Width, original.Height, _log);
                bgResizeDialog.Closed += (sender, args) =>
                {
                    if (bgResizeDialog.SaveChanges)
                    {
                        try
                        {
                            LoopyProgressTracker tracker = new();
                            _ = new ProgressDialog(() => _bg.SetBackground(bgResizeDialog.FinalImage, tracker, _log),
                                () => Content = GetEditorPanel(), tracker, string.Format(Application.Instance.Localize(this, "Replacing {0}..."), _bg.DisplayName));
                            UpdateTabTitle(false);
                        }
                        catch (Exception ex)
                        {
                            _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to replace background {0} with file {1}"), _bg.DisplayName, openFileDialog.FileName), ex);
                        }
                    }
                };
                bgResizeDialog.ShowModal();
            }
        }
    }
}
