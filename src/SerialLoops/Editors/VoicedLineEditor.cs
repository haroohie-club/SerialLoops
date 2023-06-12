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
    public class VoicedLineEditor : Editor
    {
        private VoicedLineItem _vce;
        public SoundPlayerPanel VcePlayer { get; set; }
        private readonly StackLayout _subtitlesPreview = new() { Items = { new SKGuiImage(new(256, 384)) } };
        private string _subtitle;

        public VoicedLineEditor(VoicedLineItem vce, Project project, ILogger log) : base(vce, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _vce = (VoicedLineItem)Description;
            VcePlayer = new(_vce, _log);

            Button extractButton = new() { Text = "Extract" };
            extractButton.Click += (obj, args) =>
            {
                SaveFileDialog saveFileDialog = new() { Title = "Save voiced line as WAV" };
                saveFileDialog.Filters.Add(new() { Name = "WAV File", Extensions = new string[] { ".wav" } });
                if (saveFileDialog.ShowAndReportIfFileSelected(this))
                {
                    WaveFileWriter.CreateWaveFile(saveFileDialog.FileName, _vce.GetWaveProvider(_log));
                }
            };

            Button replaceButton = new() { Text = "Replace" };
            replaceButton.Click += (obj, args) =>
            {
                OpenFileDialog openFileDialog = new() { Title = "Replace voiced line" };
                openFileDialog.Filters.Add(new() { Name = "Supported Audio Files", Extensions = new string[] { ".wav", ".flac", ".mp3", ".ogg" } });
                openFileDialog.Filters.Add(new() { Name = "WAV files", Extensions = new string[] { ".wav" } });
                openFileDialog.Filters.Add(new() { Name = "FLAC files", Extensions = new string[] { ".flac" } });
                openFileDialog.Filters.Add(new() { Name = "MP3 files", Extensions = new string[] { ".mp3" } });
                openFileDialog.Filters.Add(new() { Name = "Vorbis files", Extensions = new string[] { ".ogg" } });
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new();
                    VcePlayer.Stop();
                    _ = new ProgressDialog(() => _vce.Replace(openFileDialog.FileName, _project.BaseDirectory, _project.IterativeDirectory, Path.Combine(_project.Config.CachesDirectory, "vce", $"{_vce.Name}.wav"), _log), () =>
                    {
                        Content = GetEditorPanel();
                    }, tracker, "Replace voiced line");
                }
            };

            Button restoreButton = new() { Text = "Restore" };
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
                    PlaceholderText = "Enter subtitle text..."
                };

                ScreenSelector screenSelector = new(_log, ScreenScriptParameter.DsScreen.BOTTOM, false);
                RadioButtonList yPosSelectionList = new()
                {
                    Orientation = Orientation.Vertical,
                    Items =
                    {
                        VoiceMapFile.VoiceMapStruct.YPosition.TOP.ToString(),
                        VoiceMapFile.VoiceMapStruct.YPosition.BELOW_TOP.ToString(),
                        VoiceMapFile.VoiceMapStruct.YPosition.ABOVE_BOTTOM.ToString(),
                        VoiceMapFile.VoiceMapStruct.YPosition.BOTTOM.ToString(),
                    },
                    SelectedKey = VoiceMapFile.VoiceMapStruct.YPosition.ABOVE_BOTTOM.ToString(),
                };
                var voiceMapStruct = _project.VoiceMap.VoiceMapStructs.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                if (voiceMapStruct is not null)
                {
                    if (_project.LangCode != "ja")
                    {
                        subtitleBox.Text = voiceMapStruct.Subtitle.GetSubstitutedString(_project);
                    }
                    else
                    {
                        subtitleBox.Text = voiceMapStruct.Subtitle;
                    }
                    _subtitle = subtitleBox.Text;

                    screenSelector.SelectedScreen = voiceMapStruct.TargetScreen == VoiceMapFile.VoiceMapStruct.Screen.TOP 
                        ? ScreenScriptParameter.DsScreen.TOP : ScreenScriptParameter.DsScreen.BOTTOM;
                    yPosSelectionList.SelectedKey = voiceMapStruct.YPos.ToString();
                }

                subtitleBox.TextChanged += (sender, args) =>
                {
                    _subtitle = _project.LangCode != "ja" ? subtitleBox.Text.GetOriginalString(_project) : subtitleBox.Text;
                    var voiceMapStruct = _project.VoiceMap.VoiceMapStructs.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                    if (voiceMapStruct is null)
                    {
                        _project.VoiceMap.VoiceMapStructs.Add(new()
                        {
                            VoiceFileName = Path.GetFileNameWithoutExtension(_vce.VoiceFile),
                            FontSize = 100,
                            TargetScreen = screenSelector.SelectedScreen == ScreenScriptParameter.DsScreen.TOP
                                ? VoiceMapFile.VoiceMapStruct.Screen.TOP : VoiceMapFile.VoiceMapStruct.Screen.BOTTOM,
                            Timer = 350,
                        });
                        _project.VoiceMap.VoiceMapStructs[^1].SetSubtitle(_subtitle, _project.FontReplacement);
                        _project.VoiceMap.VoiceMapStructs[^1].YPos = Enum.Parse<VoiceMapFile.VoiceMapStruct.YPosition>(yPosSelectionList.SelectedKey);
                    }
                    else
                    {
                        voiceMapStruct.SetSubtitle(_subtitle, _project.FontReplacement);
                    }
                    UpdateTabTitle(false, subtitleBox);
                    UpdatePreview();
                };

                screenSelector.ScreenChanged += (sender, args) =>
                {
                    var voiceMapStruct = _project.VoiceMap.VoiceMapStructs.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                    if (voiceMapStruct is not null)
                    {
                        voiceMapStruct.TargetScreen = screenSelector.SelectedScreen == ScreenScriptParameter.DsScreen.TOP
                            ? VoiceMapFile.VoiceMapStruct.Screen.TOP : VoiceMapFile.VoiceMapStruct.Screen.BOTTOM;
                        UpdateTabTitle(false);
                        UpdatePreview();
                    }
                };

                yPosSelectionList.SelectedKeyChanged += (sender, args) =>
                {
                    var voiceMapStruct = _project.VoiceMap.VoiceMapStructs.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
                    if (voiceMapStruct is not null)
                    {
                        voiceMapStruct.YPos = Enum.Parse<VoiceMapFile.VoiceMapStruct.YPosition>(yPosSelectionList.SelectedKey);
                        UpdateTabTitle(false);
                        UpdatePreview();
                    }
                };

                subtitleLayout.Items.Add(ControlGenerator.GetControlWithLabel("Subtitle Text", subtitleBox));
                subtitleLayout.Items.Add(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Spacing = 20,
                    Items =
                    {
                        ControlGenerator.GetControlWithLabel("Target Screen", screenSelector),
                        ControlGenerator.GetControlWithLabel("Screen Position", yPosSelectionList)
                    }
                });
            }
            
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
                            Text = "Edit Subtitle",
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

            var voiceMapStruct = _project.VoiceMap.VoiceMapStructs.FirstOrDefault(v => v.VoiceFileName == Path.GetFileNameWithoutExtension(_vce.VoiceFile));
            bool bottomScreen = voiceMapStruct.TargetScreen == VoiceMapFile.VoiceMapStruct.Screen.BOTTOM;
            if (bottomScreen)
            {
                for (int i = 0; i <= 1; i++)
                {
                    ScriptEditor.DrawText(
                        _subtitle,
                        canvas,
                        DialogueScriptParameter.Paint07,
                        _project,
                        i + voiceMapStruct.X,
                        1 + voiceMapStruct.Y + (bottomScreen ? 192 : 0),
                        false
                    );
                }
            }

            ScriptEditor.DrawText(
                _subtitle,
                canvas,
                DialogueScriptParameter.Paint00, 
                _project,
                voiceMapStruct.X,
                voiceMapStruct.Y + (bottomScreen ? 192 : 0),
                false
            );

            canvas.Flush();
            _subtitlesPreview.Items.Add(new SKGuiImage(previewBitmap));
        }

    }
}
