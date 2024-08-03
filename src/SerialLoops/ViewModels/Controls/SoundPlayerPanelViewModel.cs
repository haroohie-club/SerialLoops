using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;

namespace SerialLoops.ViewModels.Controls
{
    public class SoundPlayerPanelViewModel : ViewModelBase
    {
        private ILogger _log;
        private ISoundItem _item;
        internal SoundPlayer _player;

        [Reactive]
        public IWaveProvider Sound { get; set; }
        [Reactive]
        public SKBitmap Waveform { get; set; }
        [Reactive]
        public bool StopButtonEnabled { get; set; }
        [Reactive]
        public string PlayPauseImagePath { get; set; }
        [Reactive]
        public string TrackName { get; set; }
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
            PlayPauseImagePath = ControlGenerator.GetVectorPath("Play");
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
            PlayPauseImagePath = ControlGenerator.GetVectorPath("Play");
            _player.Stop();
            InitializePlayer();
        }

        private void PlayPause_Executed()
        {
            StopButtonEnabled = true;
            if (_player.PlaybackState == PlaybackState.Playing)
            {
                _player.Pause();
                PlayPauseImagePath = ControlGenerator.GetVectorPath("Play");
            }
            else
            {
                _player.Play();
                PlayPauseImagePath = ControlGenerator.GetVectorPath("Pause");
            }
        }

        private void Stop_Executed()
        {
            Stop();
        }
    }
}
