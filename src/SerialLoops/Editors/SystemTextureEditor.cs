﻿using Eto.Forms;
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
    public class SystemTextureEditor(SystemTextureItem systemTexture, Project project, ILogger log) : Editor(systemTexture, log, project)
    {
        private SystemTextureItem _systemTexture;

        public override Container GetEditorPanel()
        {
            _systemTexture = (SystemTextureItem)Description;

            Button exportButton = new() { Text = Application.Instance.Localize(this, "Export") };
            exportButton.Click += ExportButton_Click;

            Button replaceButton = new() { Text = Application.Instance.Localize(this, "Replace") };
            replaceButton.Click += ReplaceButton_Click;

            Button replaceWithPaletteButton = new() { Text = Application.Instance.Localize(this, "Replace with Palette") };
            replaceWithPaletteButton.Click += ReplaceWithPaletteButton_Click;

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    new SKGuiImage(_systemTexture.GetTexture()),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 3,
                        Items =
                        {
                            exportButton,
                            replaceButton,
                            replaceWithPaletteButton,
                        },
                    },
                    new GroupBox
                    {
                        Text = Application.Instance.Localize(this, "Palette"),
                        Content = new SKGuiImage(_systemTexture.Grp.GetPalette()),
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
                    _systemTexture.GetTexture().Encode(fs, SKEncodedImageFormat.Png, 1);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to export system texture {0} to file {1}"), _systemTexture.DisplayName, saveFileDialog.FileName), ex);
                }
            }
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            ReplaceImage(false);
        }

        private void ReplaceWithPaletteButton_Click(object sender, EventArgs e)
        {
            ReplaceImage(true);
        }

        private void ReplaceImage(bool replacePalette)
        {
            OpenFileDialog openFileDialog = new();
            SKBitmap original = _systemTexture.GetTexture();
            openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "Supported Images"), Extensions = [".bmp", ".gif", ".heif", ".jpg", ".jpeg", ".png", ".webp",] });
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                ImageCropResizeDialog systemTextureResizeDialog = new(SKBitmap.Decode(openFileDialog.FileName), original.Width, original.Height, _log);
                systemTextureResizeDialog.Closed += (sender, args) =>
                {
                    if (systemTextureResizeDialog.SaveChanges)
                    {
                        try
                        {
                            LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s));
                            _ = new ProgressDialog(() => _systemTexture.SetTexture(systemTextureResizeDialog.FinalImage, replacePalette),
                                () => Content = GetEditorPanel(), tracker, string.Format(Application.Instance.Localize(this, $"Replacing {0}..."), _systemTexture.DisplayName));
                            UpdateTabTitle(false);
                        }
                        catch (Exception ex)
                        {
                            _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to replace system texture {0} with file {1}"), _systemTexture.DisplayName, openFileDialog.FileName), ex);
                        }
                    }
                };
                systemTextureResizeDialog.ShowModal();
            }
        }
    }
}
