using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace SerialLoops.Editors
{
    public class CharacterSpriteEditor(CharacterSpriteItem item, Project project, ILogger log) : Editor(item, log, project)
    {
        private CharacterSpriteItem _sprite;
        private AnimatedImage _animatedImage;

        public override Container GetEditorPanel()
        {
            _sprite = (CharacterSpriteItem)Description;
            _animatedImage = new AnimatedImage(_sprite.GetLipFlapAnimation(_project));
            _animatedImage.Play();

            Button exportFramesButton = new() { Text = Application.Instance.Localize(this, "Export Frames") };
            exportFramesButton.Click += (sender, args) =>
            {
                SelectFolderDialog folderDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Select character sprite export folder")
                };
                if (folderDialog.ShowAndReportIfFolderSelected(this))
                {
                    SKBitmap layout = _sprite.GetBaseLayout(_project);
                    List<(SKBitmap frame, short timing)> eyeFrames = _sprite.GetEyeFrames(_project);
                    List<(SKBitmap frame, short timing)> mouthFrames = _sprite.GetMouthFrames(_project);

                    try
                    {
                        using FileStream layoutStream = File.Create(Path.Combine(folderDialog.Directory, $"{_sprite.DisplayName}_BODY.png"));
                        layout.Encode(layoutStream, SKEncodedImageFormat.Png, 1);
                    }
                    catch (Exception ex)
                    {
                        _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to export layout for sprite {0} to file"), _sprite.DisplayName), ex);
                    }

                    int i = 0;
                    foreach ((SKBitmap frame, short timing) in eyeFrames)
                    {
                        try
                        {
                            using FileStream frameStream = File.Create(Path.Combine(folderDialog.Directory, $"{_sprite.DisplayName}_EYE_{i++:D3}_{timing}f.png"));
                            frame.Encode(frameStream, SKEncodedImageFormat.Png, 1);
                        }
                        catch (Exception ex)
                        {
                            _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to export eye animation {0} for sprite {1} to file"), i, _sprite.DisplayName), ex);
                        }
                    }
                    i = 0;
                    foreach ((SKBitmap frame, short timing) in mouthFrames)
                    {
                        try
                        {
                            using FileStream frameStream = File.Create(Path.Combine(folderDialog.Directory, $"{_sprite.DisplayName}_MOUTH_{i++:D3}_{timing}f.png"));
                            frame.Encode(frameStream, SKEncodedImageFormat.Png, 1);
                        }
                        catch (Exception ex)
                        {
                            _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to export mouth animation {0} for sprite {1} to file:"), i, _sprite.DisplayName), ex);
                        }
                    }
                    MessageBox.Show(Application.Instance.Localize(this, "Character sprite frames exported!"), Application.Instance.Localize(this, "Success!"), MessageBoxType.Information);
                }
            };

            Button exportGifButton = new() { Text = Application.Instance.Localize(this, "Export GIF") };
            exportGifButton.Click += (sender, args) =>
            {
                List<(SKBitmap bitmap, int timing)> animationFrames;
                if (MessageBox.Show(Application.Instance.Localize(this, "Include lip flap animation?"), Application.Instance.Localize(this, "Animation Export Option"), MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
                {
                    animationFrames = _sprite.GetLipFlapAnimation(_project);
                }
                else
                {
                    animationFrames = _sprite.GetClosedMouthAnimation(_project);
                }

                SaveFileDialog saveFileDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Save character sprite GIF"),
                };
                saveFileDialog.Filters.Add(new(Application.Instance.Localize(this, "GIF file"), ".gif"));

                if (saveFileDialog.ShowAndReportIfFileSelected(this))
                {
                    List<SKBitmap> frames = new();
                    foreach ((SKBitmap frame, int timing) in animationFrames)
                    {
                        for (int i = 0; i < timing; i++)
                        {
                            frames.Add(frame);
                        }
                    }

                    LoopyProgressTracker tracker = new();
                    _ = new ProgressDialog(() => frames.SaveGif(saveFileDialog.FileName, tracker), () => MessageBox.Show(Application.Instance.Localize(this, "GIF exported!"), Application.Instance.Localize(this, "Success!"), MessageBoxType.Information), tracker, Application.Instance.Localize(this, "Exporting GIF..."));
                }
            };

            return new StackLayout
            {
                Spacing = 10,
                Items =
                {
                    _animatedImage,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 3,
                        Items =
                        {
                            exportFramesButton,
                            exportGifButton,
                        },
                    }
                },
            };
        }
    }
}
