using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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
        public ILogger Log { get; set; }

        [Reactive]
        public string Title { get; set; }
        public uint MaxSample => (uint)(LoopPreview.Wave.Length / LoopPreview.Wave.WaveFormat.BitsPerSample * 8 / LoopPreview.Wave.WaveFormat.Channels);

        public BgmLoopPreviewItem LoopPreview { get; set; }
        public SKBitmap Waveform { get; set; }
        public SoundPlayerPanelViewModel LoopPreviewPlayer { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public BgmLoopPropertiesDialogViewModel(WaveStream wav, string title, ILogger log, bool loopEnabled, uint startSample = 0, uint endSample = 0)
        {
            Log = log;
            Title = string.Format(Strings._0____Manage_Loop, title);
            Waveform = WaveformRenderer.Render(wav, WaveFormRendererSettings.StandardSettings);
            if (endSample == 0)
            {
                endSample = (uint)(wav.Length / (wav.WaveFormat.BitsPerSample / 8));
            }
            LoopPreview = new(wav, loopEnabled, startSample, endSample);
            LoopPreviewPlayer = new(LoopPreview, Log, null);

            SaveCommand = ReactiveCommand.Create<BgmLoopPropertiesDialog>((dialog) => dialog.Close(LoopPreview));
            CancelCommand = ReactiveCommand.Create<BgmLoopPropertiesDialog>((dialog) => dialog.Close());
        }
    }
}
