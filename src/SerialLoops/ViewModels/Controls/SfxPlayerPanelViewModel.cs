using System.Windows.Input;
using GotaSequenceLib.Playback;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.ViewModels.Controls
{
    public class SfxPlayerPanelViewModel : ViewModelBase
    {
        private ILogger _log;
        private Player _player;

        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        [Reactive]
        public string PlayPauseImagePath { get; set; } = "avares://SerialLoops/Assets/Icons/Play.svg";
        [Reactive]
        public bool StopButtonEnabled { get; set; } = false;

        public SfxPlayerPanelViewModel(Player player, ILogger log)
        {
            _log = log;
            _player = player;
            _player.SongEnded += Stop;

            PlayPauseCommand = ReactiveCommand.Create(PlayPause);
            StopCommand = ReactiveCommand.Create(Stop);
        }

        private void PlayPause()
        {
            StopButtonEnabled = true;
            if (_player.State == PlayerState.Playing)
            {
                _player.Pause();
                PlayPauseImagePath = "avares://SerialLoops/Assets/Icons/Play.svg";
            }
            else
            {
                _player.Play();
                PlayPauseImagePath = "avares://SerialLoops/Assets/Icons/Pause.svg";
            }
        }

        public void Stop()
        {
            StopButtonEnabled = false;
            PlayPauseImagePath = "avares://SerialLoops/Assets/Icons/Play.svg";
            _player.Stop();
        }
    }
}
