using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaEdit.Utils;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.ViewModels.Panels;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class PuzzleEditorViewModel : EditorViewModel
{
    private PuzzleItem _puzzle;
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<TopicWithUnknown> AssociatedMainTopics { get; } = [];
    public ObservableCollection<string> HaruhiRoutes { get; } = [];

    private MapItem _map;
    public MapItem Map
    {
        get => _map;
        set
        {
            this.RaiseAndSetIfChanged(ref _map, value);
            _puzzle.Puzzle.Settings.MapId = _map.Map.Index;
            _puzzle.UnsavedChanges = true;
        }
    }

    public int BaseTime
    {
        get => _puzzle.Puzzle.Settings.BaseTime;
        set
        {
            _puzzle.Puzzle.Settings.BaseTime = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }

    public int NumSingularities
    {
        get => _puzzle.Puzzle.Settings.NumSingularities;
        set
        {
            _puzzle.Puzzle.Settings.NumSingularities = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }

    public int Unknown04
    {
        get => _puzzle.Puzzle.Settings.Unknown04;
        set
        {
            _puzzle.Puzzle.Settings.Unknown04 = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }

    public int TargetNumber
    {
        get => _puzzle.Puzzle.Settings.TargetNumber;
        set
        {
            _puzzle.Puzzle.Settings.TargetNumber = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }

    public bool ContinueOnFailure
    {
        get => _puzzle.Puzzle.Settings.ContinueOnFailure;
        set
        {
            _puzzle.Puzzle.Settings.ContinueOnFailure = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }

    public ObservableCollection<CharacterFilter> Characters { get; }

    private CharacterFilter _accompanyingCharacter;
    public CharacterFilter AccompanyingCharacter
    {
        get => _accompanyingCharacter;
        set
        {
            this.RaiseAndSetIfChanged(ref _accompanyingCharacter, value);
            _puzzle.Puzzle.Settings.AccompanyingCharacter = _accompanyingCharacter?.Character?.MessageInfo.Character ?? 0;
            _puzzle.UnsavedChanges = true;
        }
    }
    private CharacterFilter _powerCharacter1;
    public CharacterFilter PowerCharacter1
    {
        get => _powerCharacter1;
        set
        {
            this.RaiseAndSetIfChanged(ref _powerCharacter1, value);
            _puzzle.Puzzle.Settings.PowerCharacter1 = _powerCharacter1?.Character?.MessageInfo.Character ?? 0;
            _puzzle.UnsavedChanges = true;
        }
    }
    private CharacterFilter _powerCharacter2;
    public CharacterFilter PowerCharacter2
    {
        get => _powerCharacter2;
        set
        {
            this.RaiseAndSetIfChanged(ref _powerCharacter2, value);
            _puzzle.Puzzle.Settings.PowerCharacter1 = _powerCharacter2?.Character?.MessageInfo.Character ?? 0;
            _puzzle.UnsavedChanges = true;
        }
    }

    public SKBitmap Singularity => _puzzle.SingularityImage;

    public int TopicSet
    {
        get => _puzzle.Puzzle.Settings.TopicSet;
        set
        {
            _puzzle.Puzzle.Settings.TopicSet = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }

    public int Unknown15
    {
        get => _puzzle.Puzzle.Settings.Unknown15;
        set
        {
            _puzzle.Puzzle.Settings.Unknown15 = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }
    public int Unknown16
    {
        get => _puzzle.Puzzle.Settings.Unknown16;
        set
        {
            _puzzle.Puzzle.Settings.Unknown16 = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }
    public int Unknown17
    {
        get => _puzzle.Puzzle.Settings.Unknown17;
        set
        {
            _puzzle.Puzzle.Settings.Unknown17 = value;
            this.RaisePropertyChanged();
            _puzzle.UnsavedChanges = true;
        }
    }

    public PuzzleEditorViewModel(PuzzleItem puzzle, MainWindowViewModel window, ILogger log) : base(puzzle, window, log)
    {
        _puzzle = puzzle;
        Tabs = Window.EditorTabs;
        AssociatedMainTopics.AddRange(_puzzle.Puzzle.AssociatedTopics[..^1].Select(
            tu => new TopicWithUnknown(tu.Topic, tu.Unknown, Window.OpenProject)));
        HaruhiRoutes.AddRange(_puzzle.Puzzle.HaruhiRoutes.Select(r => r.ToString()));
        _map = (MapItem)Window.OpenProject.Items.FirstOrDefault(m => m.Type == ItemDescription.ItemType.Map
            && m.Name == Window.OpenProject.Dat.GetFileByName("QMAPS").CastTo<QMapFile>().QMaps[_puzzle.Puzzle.Settings.MapId].Name[..^2]);

        Characters = new(
        [
            new(null), new(Window.OpenProject.GetCharacterBySpeaker(Speaker.KYON)),
            new(Window.OpenProject.GetCharacterBySpeaker(Speaker.HARUHI)),
            new(Window.OpenProject.GetCharacterBySpeaker(Speaker.MIKURU)),
            new(Window.OpenProject.GetCharacterBySpeaker(Speaker.NAGATO)),
            new(Window.OpenProject.GetCharacterBySpeaker(Speaker.KOIZUMI)),
            new(Window.OpenProject.GetCharacterBySpeaker(Speaker.UNKNOWN)),
        ]);
        _accompanyingCharacter = Characters.First(c => (c.Character?.MessageInfo.Character ?? 0) == _puzzle.Puzzle.Settings.AccompanyingCharacter);
        _powerCharacter1 = Characters.First(c => (c.Character?.MessageInfo.Character ?? 0) == _puzzle.Puzzle.Settings.PowerCharacter1);;
        _powerCharacter2 = Characters.First(c => (c.Character?.MessageInfo.Character ?? 0) == _puzzle.Puzzle.Settings.PowerCharacter2);;
    }
}

public class TopicWithUnknown(int topicId, int unknown, Project project) : ReactiveObject
{
    [Reactive]
    public TopicItem Topic { get; set; } = (TopicItem)project.Items.FirstOrDefault(t => t.Type == ItemDescription.ItemType.Topic &&
                                                                             ((TopicItem)t).TopicEntry.Id == (short)topicId);
    [Reactive]
    public int Unknown { get; set; } = unknown;
}
