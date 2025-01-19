using System.IO;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Util;
using SerialLoops.Lib.Util.WaveformRenderer;
using SerialLoops.Models;
using SerialLoops.ViewModels.Controls;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs;

public class BgmVolumePropertiesDialogViewModel : ViewModelBase
{
    private ILogger _log;
    private WaveStream _wav;
    private long _waveLength;

    [Reactive]
    public string Title { get; set; }
    public BgmVolumePreviewItem VolumePreview { get; set; }
    [Reactive]
    public SKBitmap Waveform { get; set; }
    public SoundPlayerPanelViewModel VolumePreviewPlayer { get; set; }

    private double _volume;
    public double Volume
    {
        get => _volume;
        set
        {
            this.RaiseAndSetIfChanged(ref _volume, value);
            _wav.Seek(0, SeekOrigin.Begin);
            VolumePreview.SetVolume(Volume);
            _wav.Seek(0, SeekOrigin.Begin);
            Waveform = WaveformRenderer.Render(VolumePreview.Provider, _waveLength, WaveFormRendererSettings.StandardSettings);
            _wav.Seek(0, SeekOrigin.Begin);
            VolumePreviewPlayer.Stop();
        }
    }
    public ICommand NormalizeCommand { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    private readonly float _maxAmplitude;

    public BgmVolumePropertiesDialogViewModel(WaveStream wav, string title, float maxAmplitude, ILogger log)
    {
        _log = log;
        Title = string.Format(Strings._0____Adjust_Volume, title);
        _wav = wav;
        _volume = 100;
        Waveform = WaveformRenderer.Render(wav, WaveFormRendererSettings.StandardSettings);
        _waveLength = wav.Length;
        VolumePreview = new(wav);
        VolumePreviewPlayer = new(VolumePreview, _log, null);
        _maxAmplitude = maxAmplitude;

        NormalizeCommand = ReactiveCommand.Create(Normalize);
        SaveCommand = ReactiveCommand.Create<BgmVolumePropertiesDialog>((dialog) => dialog.Close(VolumePreview));
        CancelCommand = ReactiveCommand.Create<BgmVolumePropertiesDialog>((dialog) => dialog.Close());
    }

    private void Normalize()
    {
        VolumePreviewPlayer.Stop();
        float volumeNormalizationFactor = _maxAmplitude / VolumePreview.Provider.GetMaxAmplitude();
        if (volumeNormalizationFactor < 1)
        {
            Volume *= volumeNormalizationFactor;
        }
    }
}
