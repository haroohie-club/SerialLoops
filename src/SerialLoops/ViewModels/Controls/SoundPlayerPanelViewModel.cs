using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;

namespace SerialLoops.ViewModels.Controls
{
    public class SoundPlayerPanelViewModel : ViewModelBase
    {
        private ILogger _log;
        private ISoundItem _item;
        private SoundPlayer _player;

        private IWaveProvider _sound;
        private SKBitmap _waveform;
        private bool _stopButtonEnabled;
        private Bitmap _playPauseImage = new(AssetLoader.Open(new("avares://SerialLoops/Assets/Icons/Play.png")));
        private string _trackName;

        public IWaveProvider Sound
        {
            get => _sound;
            set => SetProperty(ref _sound, value);
        }
        public SKBitmap Waveform
        {
            get => _waveform;
            set => SetProperty(ref _waveform, value);
        }
        public bool StopButtonEnabled
        {
            get => _stopButtonEnabled;
            set => SetProperty(ref _stopButtonEnabled, value);
        }
        public Bitmap PlayPauseImage
        {
            get => _playPauseImage;
            set => SetProperty(ref _playPauseImage, value);
        }
        public string TrackName
        {
            get => _trackName;
            set => SetProperty(ref _trackName, value);
        }
        public ICommand TrackNameCommand { get; set; }
        public bool UseTextBoxForTrackName => TrackNameCommand is not null;
        public string TrackDetails { get; set; }
        public short? TrackFlag { get; set; }

        public ICommand PlayPauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public SoundPlayerPanelViewModel(ISoundItem item, ILogger log, string trackName, string trackDetails = null, short? trackFlag = null, ICommand trackNameCommand = null)
        {
            _log = log;
            _item = item;
            TrackName = trackName;
            TrackDetails = trackDetails;
            TrackFlag = trackFlag;
            TrackNameCommand = trackNameCommand;
            InitializePlayer();
            PlayPauseCommand = ReactiveCommand.Create(PlayPause_Executed);
            StopCommand = ReactiveCommand.Create(Stop_Executed);
            _player.PlaybackStopped += (sender, args) =>
            {
                Stop();
            };
        }

        private void InitializePlayer()
        {
            _log.Log("Attempting to initialize sound player...");
            Sound = _item.GetWaveProvider(_log, true);
            _player = new(Sound);
            _log.Log("Sound player successfully initialized.");
        }
        public void Stop()
        {
            StopButtonEnabled = false;
            PlayPauseImage = ControlGenerator.GetIcon("Play", _log, 25);
            _player.Stop();
            InitializePlayer();
        }

        private void PlayPause_Executed()
        {
            StopButtonEnabled = true;
            if (_player.PlaybackState == PlaybackState.Playing)
            {
                _player.Pause();
                PlayPauseImage = ControlGenerator.GetIcon("Play", _log, 25);
            }
            else
            {
                _player.Play();
                PlayPauseImage = ControlGenerator.GetIcon("Pause", _log, 25);
            }
        }

        private void Stop_Executed()
        {
            Stop();
        }
    }
}
