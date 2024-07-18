﻿using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SerialLoops.Editors
{
    public partial class CharacterSpriteEditor(CharacterSpriteItem item, Project project, ILogger log) : Editor(item, log, project)
    {
        private CharacterSpriteItem _sprite;
        private AnimatedImage _animatedImage;

        public override Container GetEditorPanel()
        {
            _sprite = (CharacterSpriteItem)Description;
            _animatedImage = new AnimatedImage(_sprite.GetLipFlapAnimation(_project));
            _animatedImage.Play();

            DropDown characterDropDown = new();
            characterDropDown.Items.AddRange(_project.Characters.Select(c => new ListItem { Key = c.Key.ToString(), Text = c.Value.Name }));
            characterDropDown.SelectedKey = ((int)_sprite.Sprite.Character).ToString();
            characterDropDown.SelectedKeyChanged += (sender, args) =>
            {
                _sprite.Sprite.Character = (Speaker)int.Parse(characterDropDown.SelectedKey);
                UpdateTabTitle(false, this);
            };

            CheckBox isLargeCheckBox = new()
            {
                Text = "Is Large",
                Checked = _sprite.Sprite.IsLarge,
            };
            isLargeCheckBox.CheckedChanged += (sender, args) =>
            {
                _sprite.Sprite.IsLarge = isLargeCheckBox.Checked ?? true;
                if (_sprite.Sprite.IsLarge)
                {
                    _sprite.Graphics.BodyTextures[2].RenderHeight *= 2;
                    _sprite.Graphics.BodyTextures[2].RenderWidth *= 2;
                    _sprite.Graphics.EyeTexture.RenderWidth *= 2;
                    _sprite.Graphics.EyeTexture.RenderHeight *= 2;
                    _sprite.Graphics.MouthTexture.RenderWidth *= 2;
                    _sprite.Graphics.MouthTexture.RenderHeight *= 2;
                }
                else
                {
                    _sprite.Graphics.BodyTextures[2].RenderWidth /= 2;
                    _sprite.Graphics.BodyTextures[2].RenderHeight /= 2;
                    _sprite.Graphics.EyeTexture.RenderWidth /= 2;
                    _sprite.Graphics.EyeTexture.RenderHeight /= 2;
                    _sprite.Graphics.MouthTexture.RenderWidth /= 2;
                    _sprite.Graphics.MouthTexture.RenderHeight /= 2;
                }
                UpdateTabTitle(false, this);
            };

            Button replaceSpriteButton = new() { Text = Application.Instance.Localize(this, "Replace") };
            replaceSpriteButton.Click += (sender, args) =>
            {
                OpenFileDialog baseFileDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Select sprite base"),
                    CheckFileExists = true,
                    Filters = { new(Application.Instance.Localize(this, "Image Files"), ".png", ".jpg", ".jpeg", ".bmp", ".gif") },
                };
                if (baseFileDialog.ShowAndReportIfFileSelected(this))
                {
                    SKBitmap baseLayout = SKBitmap.Decode(baseFileDialog.FileName);
                    Match eyePosMatch = EyePosRegex().Match(Path.GetFileNameWithoutExtension(baseFileDialog.FileName));
                    Match mouthPosMatch = MouthPosRegex().Match(Path.GetFileNameWithoutExtension(baseFileDialog.FileName));
                    short eyePosX = eyePosMatch.Success ? short.Parse(eyePosMatch.Groups["x"].Value) : (short)0;
                    short eyePosY = eyePosMatch.Success ? short.Parse(eyePosMatch.Groups["y"].Value) : (short)0;
                    short mouthPosX = mouthPosMatch.Success ? short.Parse(mouthPosMatch.Groups["x"].Value) : (short)0;
                    short mouthPosY = mouthPosMatch.Success ? short.Parse(mouthPosMatch.Groups["y"].Value) : (short)0;

                    OpenFileDialog eyeFileDialog = new()
                    {
                        Title = Application.Instance.Localize(this, "Select eye animation frames"),
                        MultiSelect = true,
                        CheckFileExists = true,
                        Filters = { new(Application.Instance.Localize(this, "Image Files"), ".png", ".jpg", ".jpeg", ".bmp", ".gif") },
                    };
                    if (eyeFileDialog.ShowAndReportIfFileSelected(this))
                    {
                        List<SKBitmap> eyeFrames = eyeFileDialog.Filenames.OrderBy(f => f).Select(f => SKBitmap.Decode(f)).ToList();
                        short[] eyeTimings = new short[eyeFrames.Count];
                        Array.Fill<short>(eyeTimings, 32);
                        for (int i = 0; i < eyeTimings.Length; i++)
                        {
                            if (Path.GetFileNameWithoutExtension(eyeFileDialog.Filenames.ElementAt(i)).EndsWith("f", StringComparison.OrdinalIgnoreCase))
                            {
                                eyeTimings[i] = short.Parse(Path.GetFileNameWithoutExtension(eyeFileDialog.Filenames.ElementAt(i)).Split('_').Last()[0..^1]);
                            }
                        }
                        List<(SKBitmap Frame, short Timing)> eyeFramesAndTimings = eyeFrames.Zip(eyeTimings).ToList();

                        OpenFileDialog mouthFileDialog = new()
                        {
                            Title = Application.Instance.Localize(this, "Select mouth animation frames"),
                            MultiSelect = true,
                            CheckFileExists = true,
                            Filters = { new(Application.Instance.Localize(this, "Image Files"), ".png", ".jpg", ".jpeg", ".bmp", ".gif") },
                        };
                        if (mouthFileDialog.ShowAndReportIfFileSelected(this))
                        {
                            List<SKBitmap> mouthFrames = mouthFileDialog.Filenames.OrderBy(f => f).Select(f => SKBitmap.Decode(f)).ToList();
                            short[] mouthTimings = new short[mouthFrames.Count];
                            for (int i = 0; i < mouthTimings.Length; i++)
                            {
                                Match frameMatch = FrameCountRegex().Match(Path.GetFileNameWithoutExtension(mouthFileDialog.Filenames.ElementAt(i)));
                                if (frameMatch.Success)
                                {
                                    mouthTimings[i] = short.Parse(frameMatch.Groups["frameCount"].Value);
                                }
                            }
                            Array.Fill<short>(mouthTimings, 32);
                            List<(SKBitmap Frame, short Timing)> mouthFramesAndTimings = mouthFrames.Zip(mouthTimings).ToList();

                            _sprite.SetSprite(baseLayout, eyeFramesAndTimings, mouthFramesAndTimings, eyePosX, eyePosY, mouthPosX, mouthPosY);
                            UpdateTabTitle(false, this);
                        }
                    }
                }
            };

            Button exportFramesButton = new() { Text = Application.Instance.Localize(this, "Export Frames") };
            exportFramesButton.Click += (sender, args) =>
            {
                SelectFolderDialog folderDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Select character sprite export folder")
                };
                if (folderDialog.ShowAndReportIfFolderSelected(this))
                {
                    SKBitmap layout = _sprite.GetBaseLayout();
                    List<(SKBitmap frame, short timing)> eyeFrames = _sprite.GetEyeFrames();
                    List<(SKBitmap frame, short timing)> mouthFrames = _sprite.GetMouthFrames();

                    try
                    {
                        using FileStream layoutStream = File.Create(Path.Combine(folderDialog.Directory, $"{_sprite.DisplayName}_BODY" +
                            $"_E{_sprite.Graphics.EyeAnimation.AnimationX},{_sprite.Graphics.EyeAnimation.AnimationY}" +
                            $"_M{_sprite.Graphics.MouthAnimation.AnimationX},{_sprite.Graphics.MouthAnimation.AnimationY}.png"));
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
                    List<SKBitmap> frames = [];
                    foreach ((SKBitmap frame, int timing) in animationFrames)
                    {
                        for (int i = 0; i < timing; i++)
                        {
                            frames.Add(frame);
                        }
                    }

                    LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s));
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
                        Spacing = 5,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("Character", characterDropDown),
                            isLargeCheckBox,
                        }
                    },
                    replaceSpriteButton,
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

        [GeneratedRegex(@"_(?<frameCount>\d{1,4})f(?:_|$)", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex FrameCountRegex();
        [GeneratedRegex(@"_E(?<x>\d{1,3}),(?<y>\d{1,3})(?:_|$)")]
        private static partial Regex EyePosRegex();
        [GeneratedRegex(@"_M(?<x>\d{1,3}),(?<y>\d{1,3})(?:_|$)")]
        private static partial Regex MouthPosRegex();
    }
}
