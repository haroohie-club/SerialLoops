﻿using Eto.Forms;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class VoicedLineEditor : Editor
    {
        private VoicedLineItem _vce;
        public SoundPlayerPanel VcePlayer { get; set; }

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
                openFileDialog.Filters.Add(new() { Name = "WAV File", Extensions = new string[] { ".wav" } });
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new();
                    _ = new ProgressDialog(() => _vce.Replace(openFileDialog.FileName, _project.BaseDirectory, _project.IterativeDirectory), () =>
                    {
                        Content = GetEditorPanel();
                    }, tracker, "Replace voiced line");
                }
            };

            return new TableLayout(
                new TableRow(ControlGenerator.GetPlayerStackLayout(VcePlayer, _vce.Name, _vce.AdxType.ToString())),
                new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Items =
                    {
                        replaceButton,
                        extractButton,
                    }
                }),
                new TableRow()
                );
        }
    }
}
