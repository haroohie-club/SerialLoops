﻿using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Util.WaveformRenderer;
using SerialLoops.Models;
using SerialLoops.ViewModels.Controls;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs
{
    public class BgmLoopPropertiesDialogViewModel : ViewModelBase
    {
        private ILogger _log;

        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public uint MaxSample => (uint)(LoopPreview.Wave.Length / LoopPreview.Wave.WaveFormat.BitsPerSample * 8 / LoopPreview.Wave.WaveFormat.Channels);

        public BgmLoopPreviewItem LoopPreview { get; set; }
        public SKBitmap Waveform { get; set; }
        public SoundPlayerPanelViewModel LoopPreviewPlayer { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DebugCommand { get; }

        public BgmLoopPropertiesDialogViewModel(WaveStream wav, string title, ILogger log, bool loopEnabled, uint startSample = 0, uint endSample = 0)
        {
            _log = log;
            Title = string.Format(Strings._0____Manage_Loop, title);
            Waveform = WaveformRenderer.Render(wav, WaveFormRendererSettings.StandardSettings);
            if (endSample == 0)
            {
                endSample = (uint)(wav.Length / (wav.WaveFormat.BitsPerSample / 8));
            }
            LoopPreview = new(wav, loopEnabled, startSample, endSample);
            LoopPreviewPlayer = new(LoopPreview, _log, null);

            SaveCommand = ReactiveCommand.Create<BgmLoopPropertiesDialog>((dialog) => dialog.Close(LoopPreview));
            CancelCommand = ReactiveCommand.Create<BgmLoopPropertiesDialog>((dialog) => dialog.Close());
            DebugCommand = ReactiveCommand.Create(Debug);
        }

        private void Debug()
        {
            _log.Log($"Loop Start: {LoopPreview.StartSample}; Loop End: {LoopPreview.EndSample}");
        }
    }
}