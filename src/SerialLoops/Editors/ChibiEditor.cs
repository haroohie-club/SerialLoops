using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace SerialLoops.Editors
{
    public class ChibiEditor(ChibiItem chibi, Project project, ILogger log) : Editor(chibi, log, project)
    {
        private ChibiItem _chibi;
        private DropDown _animationSelection;
        private ChibiDirectionSelector _directionSelector;
        private string _currentChibiEntry;

        private AnimatedImage _animatedImage;
        private StackLayout _framesStack;

        public override Container GetEditorPanel()
        {
            _chibi = (ChibiItem)Description;
            return new TableLayout(GetAnimatedChibi(), GetBottomPanel());
        }

        private StackLayout GetAnimatedChibi()
        {
            _animatedImage = new(_chibi.ChibiAnimations.FirstOrDefault().Value);
            _currentChibiEntry = _chibi.ChibiAnimations.FirstOrDefault().Key;
            _animatedImage.Play();
            return new StackLayout
            {
                Items = { new TableLayout(_animatedImage) },
                Height = Math.Max(250, _animatedImage.Height),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private Container GetBottomPanel()
        {
            _animationSelection = new();
            _animationSelection.Items.AddRange(_chibi.ChibiEntries
                .Select(c => c.Name[..^3]).Distinct().Select(c => new ListItem() { Text = c, Key = c }));
            _animationSelection.SelectedIndex = 0;
            _animationSelection.SelectedKeyChanged += ChibiSelection_SelectedKeyChanged;

            _directionSelector = new(_chibi, _animationSelection.SelectedKey.Trim(), _log)
            {
                Direction = ChibiItem.Direction.DOWN_LEFT
            };
            _directionSelector.DirectionChanged += ChibiSelection_SelectedKeyChanged;

            Button exportGifButton = new() { Text = Application.Instance.Localize(this, "Export GIF") };
            exportGifButton.Click += (sender, args) =>
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Save chibi GIF"),
                };
                saveFileDialog.Filters.Add(new(Application.Instance.Localize(this, "GIF file"), ".gif"));

                if (saveFileDialog.ShowAndReportIfFileSelected(this))
                {
                    List<SKBitmap> frames = [];
                    foreach ((SKGuiImage frame, int timing) in _animatedImage.FramesWithTimings)
                    {
                        for (int i = 0; i < timing; i++)
                        {
                            frames.Add(frame.SkBitmap);
                        }
                    }

                    LoopyProgressTracker tracker = new();
                    _ = new ProgressDialog(() => frames.SaveGif(saveFileDialog.FileName, tracker), () => MessageBox.Show(Application.Instance.Localize(this, "GIF exported!"), Application.Instance.Localize(this, "Success!"), MessageBoxType.Information), tracker, Application.Instance.Localize(this, "Exporting GIF..."));
                }
            };
            Button exportSpritesButton = new() { Text = Application.Instance.Localize(this, "Export Sprites") };
            exportSpritesButton.Click += (sender, args) =>
            {
                SelectFolderDialog folderDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Select chibi export folder")
                };
                if (folderDialog.ShowAndReportIfFolderSelected(this))
                {
                    int i = 0;
                    foreach ((SKGuiImage frame, int timing) in _animatedImage.FramesWithTimings)
                    {
                        try
                        {
                            using FileStream frameStream = File.Create(Path.Combine(folderDialog.Directory, $"{_chibi.DisplayName}_{_animationSelection.SelectedKey}_{_directionSelector.Direction}_{i++:D3}_{timing}f.png"));
                            frame.SkBitmap.Encode(frameStream, SKEncodedImageFormat.Png, 1);
                        }
                        catch (Exception ex)
                        {
                            _log.LogException(string.Format(Application.Instance.Localize(this, "Failed to export chibi animation {0} for chibi {1} to file"), i, _chibi.DisplayName), ex);
                        }
                    }
                    MessageBox.Show(Application.Instance.Localize(this, "Chibi frames exported!"), Application.Instance.Localize(this, "Success!"), MessageBoxType.Information);
                }
            };
            Button replaceFramesButton = new() { Text = Application.Instance.Localize(this, "Replace Frames") };
            replaceFramesButton.Click += (sender, args) =>
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Select frames"),
                    CheckFileExists = true,
                    MultiSelect = true,
                    Filters = { new(Application.Instance.Localize(this, "Image Files"), ".png", ".jpg", ".jpeg", ".bmp", ".gif") },
                };
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    List<SKBitmap> frames = openFileDialog.Filenames.Select(f => SKBitmap.Decode(f)).ToList();
                    short[] timings = new short[frames.Count];
                    Array.Fill<short>(timings, 32);
                    List<(SKBitmap Frame, short Timing)> framesAndTimings = frames.Zip(timings).ToList();
                    UpdateChibiFrames(framesAndTimings);
                }
            };
            Button addFramesButton = new() { Text = Application.Instance.Localize(this, "Add Frames") };
            addFramesButton.Click += (sender, args) =>
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = Application.Instance.Localize(this, "Select frames"),
                    CheckFileExists = true,
                    MultiSelect = true,
                    Filters = { new(Application.Instance.Localize(this, "Image Files"), ".png", ".jpg", ".jpeg", ".bmp", ".gif") },
                };
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    List<SKBitmap> frames = openFileDialog.Filenames.Select(f => SKBitmap.Decode(f)).ToList();
                    short[] timings = new short[frames.Count];
                    Array.Fill<short>(timings, 32);
                    List<(SKBitmap Frame, short Timing)> newFramesAndTimings = frames.Zip(timings).ToList();
                    _chibi.ChibiAnimations[_currentChibiEntry].AddRange(newFramesAndTimings);
                    UpdateChibiFrames(_chibi.ChibiAnimations[_currentChibiEntry]);
                }
            };

            return new TableLayout
            {
                Padding = 10,
                Spacing = new(5, 5),
                Rows =
                {
                    new(
                        new GroupBox
                        {
                            Text = Application.Instance.Localize(this, "Animation"),
                            Padding = 10,
                            Content = new StackLayout
                            {
                                Spacing = 10,
                                Items =
                                {
                                    _animationSelection,
                                    _directionSelector
                                }
                            }
                        },
                        new GroupBox
                        {
                            Text = Application.Instance.Localize(this, "Frames"),
                            Padding = 10,
                            Content = new Scrollable { Content = GetFramesStack() }
                        }
                    ),
                    new(
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 3,
                            Items =
                            {
                                exportSpritesButton,
                                exportGifButton,
                                replaceFramesButton,
                            }
                        }
                    ),
                }
            };
        }

        private void UpdateChibiFrames(List<(SKBitmap Frame, short Timing)> framesAndTimings)
        {
            Application.Instance.Invoke(() =>
            {
                GraphicsFile animation = _chibi.ChibiEntries.First(c => c.Name == _currentChibiEntry).Chibi.Animation;
                _chibi.SetChibiAnimation(_currentChibiEntry, framesAndTimings);
                _chibi.ChibiAnimations[_currentChibiEntry].Clear();
                _chibi.ChibiAnimations[_currentChibiEntry].AddRange(_chibi.GetChibiAnimation(_currentChibiEntry, _project.Grp));
                AnimatedImage newImage = new(_chibi.ChibiAnimations[_currentChibiEntry]);
                _animatedImage.FramesWithTimings = newImage.FramesWithTimings;
                _animatedImage.CurrentFrame = 0;
                _animatedImage.UpdateImage();
                UpdateFramesStack();
                UpdateTabTitle(false);
            });
        }

        private StackLayout GetFramesStack()
        {
            _framesStack = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15
            };
            UpdateFramesStack();
            return _framesStack;
        }

        private void UpdateFramesStack()
        {
            _framesStack.Items.Clear();
            int i = 0;
            foreach ((SKGuiImage image, int timing) in _animatedImage.FramesWithTimings)
            {
                int currentFrame = i;
                StackLayout frameLayout = new()
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 3,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Items =
                    {
                        image
                    }
                };
                if (timing >= 0)
                {
                    Timer frameStackTimer = new(1000) { AutoReset = false };
                    NumericStepper timingStepper = new()
                    {
                        Value = timing,
                        MinValue = 0,
                        MaxValue = short.MaxValue,
                        Width = 50,
                        DecimalPlaces = 0,
                        MaximumDecimalPlaces = 0,
                    };
                    timingStepper.ValueChanged += (sender, args) =>
                    {
                        frameStackTimer.Stop();
                        frameStackTimer.Start();
                    };
                    frameStackTimer.Elapsed += (sender, args) =>
                    {
                        Application.Instance.Invoke(() =>
                        {
                            GraphicsFile animation = _chibi.ChibiEntries.First(c => c.Name == _currentChibiEntry).Chibi.Animation;
                            _chibi.ChibiAnimations[_currentChibiEntry][currentFrame] = (image.SkBitmap, (short)timingStepper.Value);
                            _chibi.SetChibiAnimation(_currentChibiEntry, _chibi.ChibiAnimations[_currentChibiEntry]);
                            AnimatedImage newImage = new(_chibi.ChibiAnimations[_currentChibiEntry]);
                            _animatedImage.FramesWithTimings = newImage.FramesWithTimings;
                            _animatedImage.CurrentFrame = 0;
                            _animatedImage.UpdateImage();
                            UpdateFramesStack();
                            UpdateTabTitle(false);
                        });
                    };
                    frameLayout.Items.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Frames"), timingStepper));
                }
                _framesStack.Items.Add(frameLayout);
                i++;
            }
        }

        private void ChibiSelection_SelectedKeyChanged(object sender, EventArgs e)
        {
            _directionSelector.UpdateAvailableDirections(_chibi, _animationSelection.SelectedKey.Trim());
            _currentChibiEntry = GetSelectedAnimationKey();
            if (!_chibi.ChibiAnimations.ContainsKey(_currentChibiEntry))
            {
                _currentChibiEntry = _chibi.ChibiAnimations.Keys.First();
                _directionSelector.Direction = ChibiItem.CodeToDirection(_currentChibiEntry[^2..]);
            }
            AnimatedImage newImage = new(_chibi.ChibiAnimations[_currentChibiEntry]);
            _animatedImage.FramesWithTimings = newImage.FramesWithTimings;
            _animatedImage.CurrentFrame = 0;
            _animatedImage.UpdateImage();
            UpdateFramesStack();
        }

        private string GetSelectedAnimationKey()
        {
            string direction = "";
            switch (_directionSelector.Direction)
            {
                case ChibiItem.Direction.DOWN_LEFT:
                    direction = "BL";
                    break;
                case ChibiItem.Direction.DOWN_RIGHT:
                    direction = "BR";
                    break;
                case ChibiItem.Direction.UP_LEFT:
                    direction = "UL";
                    break;
                case ChibiItem.Direction.UP_RIGHT:
                    direction = "UR";
                    break;
            }
            return $"{_animationSelection.SelectedKey.Trim()}_{direction}";
        }
    }
}
