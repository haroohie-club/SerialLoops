using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.IO;

namespace SerialLoops.Editors
{
    public class ItemEditor(ItemItem item, Project project, ILogger log) : Editor(item,log, project)
    {
        private ItemItem _item;

        public override Container GetEditorPanel()
        {
            _item = (ItemItem)Description;

            Button exportButton = new() { Text = Application.Instance.Localize(this, "Export") };
            exportButton.Click += ExportButton_Click;

            Button replaceButton = new() { Text = Application.Instance.Localize(this, "Replace") };
            replaceButton.Click += ReplaceButton_Click;

            return new Scrollable
            {
                Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 5,
                    Items =
                    {
                        new ImageView() { Image = new SKGuiImage(_item.GetImage()) },
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
                    _item.GetImage().Encode(fs, SKEncodedImageFormat.Png, 1);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to export item {0} to file {1}"), _item.DisplayName, saveFileDialog.FileName), ex);
                }
            }
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            SKBitmap original = _item.GetImage();
            openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "Supported Images"), Extensions = [".bmp", ".gif", ".heif", ".jpg", ".jpeg", ".png", ".webp",] });
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                ImageCropResizeDialog itemResizeDialog = new(SKBitmap.Decode(openFileDialog.FileName), original.Width, original.Height, _log);
                itemResizeDialog.Closed += (sender, args) =>
                {
                    if (itemResizeDialog.SaveChanges)
                    {
                        try
                        {
                            LoopyProgressTracker tracker = new();
                            _ = new ProgressDialog(() => _item.SetImage(itemResizeDialog.FinalImage, tracker, _log),
                                () => Content = GetEditorPanel(), tracker, string.Format(Application.Instance.Localize(this, "Replacing {0}..."), _item.DisplayName));
                            UpdateTabTitle(false);
                        }
                        catch (Exception ex)
                        {
                            _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to replace item {0} with file {1}"), _item.DisplayName, openFileDialog.FileName), ex);
                        }
                    }
                };
                itemResizeDialog.ShowModal();
            }
        }
    }
}
