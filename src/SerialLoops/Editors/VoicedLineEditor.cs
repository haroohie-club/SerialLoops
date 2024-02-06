using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;

namespace SerialLoops.Editors
{
    public class VoicedLineEditor(VoicedLineItem vce, Project project, ILogger log) : Editor(vce, log, project)
    {
        private VoicedLineItem _vce;
        public SoundPlayerPanel VcePlayer { get; set; }
        private readonly StackLayout _subtitlesPreview = new() { Items = { new SKGuiImage(new(256, 384)) } };
        private string _subtitle;

        public override Container GetEditorPanel()
        {
            _vce = (VoicedLineItem)Description;
            VcePlayer = new(_vce, _log);

            Button extractButton = new() { Text = Application.Instance.Localize(this, "Extract") };
            extractButton.Click += (obj, args) =>
            {
                SaveFileDialog saveFileDialog = new() { Title = Application.Instance.Localize(this, "Save voiced line as WAV") };
                saveFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "WAV File"), Extensions = [".wav"] });
                if (saveFileDialog.ShowAndReportIfFileSelected(this))
                {
                    WaveFileWriter.CreateWaveFile(saveFileDialog.FileName, _vce.GetWaveProvider(_log));
                }
            };

            Button replaceButton = new() { Text = Application.Instance.Localize(this, "Replace") };
            replaceButton.Click += (obj, args) =>
            {
                OpenFileDialog openFileDialog = new() { Title = Application.Instance.Localize(this, "Replace voiced line") };
                openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "Supported Audio Files"), Extensions = [".wav", ".flac", ".mp3", ".ogg"] });
                openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "WAV files"), Extensions = [".wav"] });
                openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "FLAC files"), Extensions = [".flac"] });
                openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "MP3 files"), Extensions = [".mp3"] });
                openFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "Vorbis files"), Extensions = [".ogg"] });
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s));
                    VcePlayer.Stop();
                    _ = new ProgressDialog(() => _vce.Replace(openFileDialog.FileName, _project.BaseDirectory, _project.IterativeDirectory, Path.Combine(_project.Config.CachesDirectory, "vce", $"{_vce.Name}.wav"), _log), () =>
                    {
                        Content = GetEditorPanel();
                    }, tracker, Application.Instance.Localize(this, "Replace voiced line"));
                }
            };

            Button restoreButton = new() { Text = Application.Instance.Localize(this, "Restore") };
            restoreButton.Click += (obj, args) =>
            {
                VcePlayer.Stop();
                File.Copy(Path.Combine(_project.BaseDirectory, "original", "vce", Path.GetFileName(_vce.VoiceFile)), Path.Combine(_project.BaseDirectory, _vce.VoiceFile), true);
                File.Copy(Path.Combine(_project.IterativeDirectory, "original", "vce", Path.GetFileName(_vce.VoiceFile)), Path.Combine(_project.IterativeDirectory, _vce.VoiceFile), true);
                Content = GetEditorPanel();
            };

            StackLayout subtitleLayout = new() { Spacing = 5 };
            if (_project.VoiceMap is not null)
            {
                TextBox subtitleBox = new()
                {
                    Width = 400,
                    PlaceholderText = Application.Instance.Localize(this, "Enter subtitle text...")
                };

                ScreenSelector screenSelector = new(_log, ScreenScriptParameter.DsScreen.BOTTOM, false);
                RadioButtonList yPosSelectionList = new()
                {
                    Orientation = Orientation.Vertical,
                    Items =
                    {
                        VoiceMapFile.VoiceMapEntry.YPosition.TOP.ToString(),
                        VoiceMapFile.VoiceMapEntry.YPosition.BELOW_TOP.ToString(),
                        VoiceMapFile.VoiceMapEntry.YPosition.ABOVE_BOTTOM.ToString(),
                        VoiceMapFile.VoiceMapEntry.YPosition.BOTTOM.ToString(),
                    },
                    SelectedKey = VoiceMapFile.VoiceMapEntry.YPosition.ABOVE_BOTTOM.ToString(),
                };
                var VoiceMapEntry = _project.VoiceMap.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                if (VoiceMapEntry is not null)
                {
                    if (_project.LangCode != "ja")
                    {
                        subtitleBox.Text = VoiceMapEntry.Subtitle.GetSubstitutedString(_project);
                    }
                    else
                    {
                        subtitleBox.Text = VoiceMapEntry.Subtitle;
                    }
                    _subtitle = _project.LangCode != "ja" ? subtitleBox.Text.GetOriginalString(_project) : subtitleBox.Text;

                    screenSelector.SelectedScreen = VoiceMapEntry.TargetScreen == VoiceMapFile.VoiceMapEntry.Screen.TOP 
                        ? ScreenScriptParameter.DsScreen.TOP : ScreenScriptParameter.DsScreen.BOTTOM;
                    yPosSelectionList.SelectedKey = VoiceMapEntry.YPos.ToString();
                }

                subtitleBox.TextChanged += (sender, args) =>
                {
                    _subtitle = _project.LangCode != "ja" ? subtitleBox.Text.GetOriginalString(_project) : subtitleBox.Text;
                    var VoiceMapEntry = _project.VoiceMap.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                    if (VoiceMapEntry is null)
                    {
                        _project.VoiceMap.VoiceMapEntries.Add(new()
                        {
                            VoiceFileName = Path.GetFileNameWithoutExtension(_vce.VoiceFile),
                            FontSize = 100,
                            TargetScreen = screenSelector.SelectedScreen == ScreenScriptParameter.DsScreen.TOP
                                ? VoiceMapFile.VoiceMapEntry.Screen.TOP : VoiceMapFile.VoiceMapEntry.Screen.BOTTOM,
                            Timer = 350,
                        });
                        _project.VoiceMap.VoiceMapEntries[^1].SetSubtitle(_subtitle, _project.FontReplacement);
                        _project.VoiceMap.VoiceMapEntries[^1].YPos = Enum.Parse<VoiceMapFile.VoiceMapEntry.YPosition>(yPosSelectionList.SelectedKey);
                    }
                    else
                    {
                        VoiceMapEntry.SetSubtitle(_subtitle, _project.FontReplacement);
                    }
                    UpdateTabTitle(false, subtitleBox);
                    UpdatePreview();
                };

                screenSelector.ScreenChanged += (sender, args) =>
                {
                    var VoiceMapEntry = _project.VoiceMap.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                    if (VoiceMapEntry is not null)
                    {
                        VoiceMapEntry.TargetScreen = screenSelector.SelectedScreen == ScreenScriptParameter.DsScreen.TOP
                            ? VoiceMapFile.VoiceMapEntry.Screen.TOP : VoiceMapFile.VoiceMapEntry.Screen.BOTTOM;
                        UpdateTabTitle(false);
                        UpdatePreview();
                    }
                };

                yPosSelectionList.SelectedKeyChanged += (sender, args) =>
                {
                    var VoiceMapEntry = _project.VoiceMap.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                    if (VoiceMapEntry is not null)
                    {
                        VoiceMapEntry.YPos = Enum.Parse<VoiceMapFile.VoiceMapEntry.YPosition>(yPosSelectionList.SelectedKey);
                        UpdateTabTitle(false);
                        UpdatePreview();
                    }
                };

                subtitleLayout.Items.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Subtitle Text"), subtitleBox));
                subtitleLayout.Items.Add(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Spacing = 20,
                    Items =
                    {
                        ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Target Screen"), screenSelector),
                        ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Screen Position"), yPosSelectionList)
                    }
                });
            }

            UpdatePreview();
            
            return new TableLayout(
                new TableRow(new TableLayout(
                        new TableRow(ControlGenerator.GetPlayerStackLayout(VcePlayer, _vce.Name, _vce.AdxType.ToString())),
                        new TableRow(new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 3,
                            Padding = 5,
                            Items =
                            {
                                replaceButton,
                                extractButton,
                                restoreButton,
                            }
                        }),
                        new TableRow(new GroupBox
                        {
                            Text = Application.Instance.Localize(this, "Edit Subtitle"),
                            Padding = 5,
                            Content = subtitleLayout
                        }),
                        new TableRow()
                    ), 
                    new TableRow(_subtitlesPreview))
            );
        }

        private void UpdatePreview()
        {
            _subtitlesPreview.Items.Clear();
            if (_project.VoiceMap is null || string.IsNullOrEmpty(_subtitle))
            {
                return;
            }

            SKBitmap previewBitmap = new(256, 384);
            SKCanvas canvas = new(previewBitmap);
            canvas.DrawColor(SKColors.DarkGray);
            canvas.DrawLine(new SKPoint { X = 0, Y = 192 }, new SKPoint { X = 256, Y = 192 }, DialogueScriptParameter.Paint00);

            var VoiceMapEntry = _project.VoiceMap.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
            bool bottomScreen = VoiceMapEntry.TargetScreen == VoiceMapFile.VoiceMapEntry.Screen.BOTTOM;
            if (bottomScreen)
            {
                for (int i = 0; i <= 1; i++)
                {
                    canvas.DrawHaroohieText(
                        _subtitle,
                        DialogueScriptParameter.Paint07,
                        _project,
                        i + VoiceMapEntry.X,
                        1 + VoiceMapEntry.Y + (bottomScreen ? 192 : 0),
                        false
                    );
                }
            }

            canvas.DrawHaroohieText(
                _subtitle,
                DialogueScriptParameter.Paint00, 
                _project,
                VoiceMapEntry.X,
                VoiceMapEntry.Y + (bottomScreen ? 192 : 0),
                false
            );

            canvas.Flush();
            _subtitlesPreview.Items.Add(new SKGuiImage(previewBitmap));
        }

    }
}
