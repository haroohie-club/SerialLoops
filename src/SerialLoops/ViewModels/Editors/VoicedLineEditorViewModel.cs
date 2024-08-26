using System.IO;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.ViewModels.Controls;
using SkiaSharp;
using static SerialLoops.Lib.Script.Parameters.ScreenScriptParameter;

namespace SerialLoops.ViewModels.Editors
{
    public class VoicedLineEditorViewModel : EditorViewModel
    {
        private VoicedLineItem _vce;
        private string _subtitle;

        [Reactive]
        public SoundPlayerPanelViewModel VcePlayer { get; set; }
        public ScreenSelectorViewModel ScreenSelector { get; set; }
        [Reactive]
        public SKBitmap SubtitlesPreview { get; set; } = new(256, 384);

        public DsScreen SubtitleScreen { get; set; }

        [Reactive]
        public bool TopY { get; set; }
        [Reactive]
        public bool BelowTopY { get; set; }
        [Reactive]
        public bool AboveBottomY { get; set; }
        [Reactive]
        public bool BottomY { get; set; }

        public string Subtitle
        {
            get => _project.LangCode.Equals("ja") ? _subtitle : (_subtitle?.GetSubstitutedString(_project) ?? string.Empty);
            set
            {
                _subtitle = _project.LangCode.Equals("ja") ? value : value.GetOriginalString(_project);
                this.RaiseAndSetIfChanged(ref _subtitle, value);
                VoiceMapFile.VoiceMapEntry voiceMapEntry = _project.VoiceMap.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName.Equals(Path.GetFileNameWithoutExtension(_vce.VoiceFile)));
                if (voiceMapEntry is null)
                {
                    _project.VoiceMap.VoiceMapEntries.Add(new()
                    {
                        VoiceFileName = Path.GetFileNameWithoutExtension(_vce.VoiceFile),
                        FontSize = 100,
                        TargetScreen = SubtitleScreen == DsScreen.BOTTOM ? VoiceMapFile.VoiceMapEntry.Screen.BOTTOM : VoiceMapFile.VoiceMapEntry.Screen.TOP,
                        Timer = 350,
                    });
                    _project.VoiceMap.VoiceMapEntries[^1].SetSubtitle(_subtitle, _project.FontReplacement);
                    _project.VoiceMap.VoiceMapEntries[^1].YPos = TopY ? VoiceMapFile.VoiceMapEntry.YPosition.TOP :
                        BelowTopY ? VoiceMapFile.VoiceMapEntry.YPosition.BELOW_TOP :
                        AboveBottomY ? VoiceMapFile.VoiceMapEntry.YPosition.ABOVE_BOTTOM : VoiceMapFile.VoiceMapEntry.YPosition.BOTTOM;
                }
                else
                {
                    voiceMapEntry.SetSubtitle(_subtitle, _project.FontReplacement);
                }
                Description.UnsavedChanges = true;
            }
        }

        public bool SubsEnabled => _window.OpenProject.VoiceMap is not null;

        public VoicedLineEditorViewModel(VoicedLineItem item, MainWindowViewModel window, ILogger log) : base(item, window, log, window.OpenProject)
        {
            _vce = item;
            VcePlayer = new(_vce, log, null);
            ScreenSelector = new(DsScreen.BOTTOM, false);

            ScreenSelector.ScreenChanged += (sender, args) =>
            {
                SubtitleScreen = ScreenSelector.SelectedScreen;
            };
        }
    }
}
