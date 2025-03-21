using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Input;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors;

public class TopicEditorViewModel : EditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public TopicItem Topic { get; }

    private string _title;

    public string Title
    {
        get => _title.GetSubstitutedString(Window.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value.GetOriginalString(Window.OpenProject));
            Topic.TopicEntry.Title = _title;
            Topic.DisplayName = $"{Topic.TopicEntry.Id - 1} - {_title.GetSubstitutedString(Window.OpenProject)}";
            Topic.UnsavedChanges = true;
        }
    }

    public ObservableCollection<ScriptItem> Scripts { get; }
    private ScriptItem _associatedScript;
    public ScriptItem AssociatedScript
    {
        get => _associatedScript;
        set
        {
            this.RaiseAndSetIfChanged(ref _associatedScript, value);
            if (Topic.TopicEntry.Type == TopicType.Main)
            {
                Topic.HiddenMainTopic.EventIndex = (short)_associatedScript.Event.Index;
            }
            else
            {
                Topic.TopicEntry.EventIndex = (short)_associatedScript.Event.Index;
            }
            Topic.UnsavedChanges = true;
        }
    }

    public ObservableCollection<string> EpisodeGroups { get; }
    private byte _episodeGroup;
    public byte EpisodeGroup
    {
        get => (byte)(_episodeGroup - 1);
        set
        {
            this.RaiseAndSetIfChanged(ref _episodeGroup, (byte)(value + 1));
            Topic.TopicEntry.EpisodeGroup = _episodeGroup;
            Topic.UnsavedChanges = true;
        }
    }

    private byte _puzzlePhaseGroup;
    public byte PuzzlePhaseGroup
    {
        get => _puzzlePhaseGroup;
        set
        {
            this.RaiseAndSetIfChanged(ref _puzzlePhaseGroup, value);
            Topic.TopicEntry.PuzzlePhaseGroup = _puzzlePhaseGroup;
            Topic.UnsavedChanges = true;
        }
    }

    private short _baseTimeGain;
    public short BaseTimeGain
    {
        get => _baseTimeGain;
        set
        {
            this.RaiseAndSetIfChanged(ref _baseTimeGain, value);
            Topic.TopicEntry.BaseTimeGain = _baseTimeGain;
            KyonTime = BaseTimeGain * _kyonTimePercentage / 100.0;
            MikuruTime = BaseTimeGain * _mikuruTimePercentage / 100.0;
            NagatoTime = BaseTimeGain * _nagatoTimePercentage / 100.0;
            KoizumiTime = BaseTimeGain * _koizumiTimePercentage / 100.0;
            Topic.UnsavedChanges = true;
        }
    }

    private short _kyonTimePercentage;
    [Reactive]
    public double KyonTime { get; set; }
    public short KyonTimePercentage
    {
        get => _kyonTimePercentage;
        set
        {
            this.RaiseAndSetIfChanged(ref _kyonTimePercentage, value);
            Topic.TopicEntry.KyonTimePercentage = _kyonTimePercentage;
            KyonTime = BaseTimeGain * _kyonTimePercentage / 100.0;
            Topic.UnsavedChanges = true;
        }
    }

    private short _mikuruTimePercentage;
    [Reactive]
    public double MikuruTime { get; set; }
    public short MikuruTimePercentage
    {
        get => _mikuruTimePercentage;
        set
        {
            this.RaiseAndSetIfChanged(ref _mikuruTimePercentage, value);
            Topic.TopicEntry.MikuruTimePercentage = _mikuruTimePercentage;
            MikuruTime = BaseTimeGain * _mikuruTimePercentage / 100.0;
            Topic.UnsavedChanges = true;
        }
    }

    private short _nagatoTimePercentage;
    [Reactive]
    public double NagatoTime { get; set; }
    public short NagatoTimePercentage
    {
        get => _nagatoTimePercentage;
        set
        {
            this.RaiseAndSetIfChanged(ref _nagatoTimePercentage, value);
            Topic.TopicEntry.NagatoTimePercentage = _nagatoTimePercentage;
            NagatoTime = BaseTimeGain * _nagatoTimePercentage / 100.0;
            Topic.UnsavedChanges = true;
        }
    }

    private short _koizumiTimePercentage;
    [Reactive]
    public double KoizumiTime { get; set; }
    public short KoizumiTimePercentage
    {
        get => _koizumiTimePercentage;
        set
        {
            this.RaiseAndSetIfChanged(ref _koizumiTimePercentage, value);
            Topic.TopicEntry.KoizumiTimePercentage = _koizumiTimePercentage;
            KoizumiTime = BaseTimeGain * _koizumiTimePercentage / 100.0;
            Topic.UnsavedChanges = true;
        }
    }

    private StackHistory _history;

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public KeyGesture UndoGesture { get; }
    public KeyGesture RedoGesture { get; }

    public short MaxShort => short.MaxValue;

    public TopicEditorViewModel(TopicItem topic, MainWindowViewModel window, ILogger log) : base(topic, window, log)
    {
        _history = new();

        Tabs = window.EditorTabs;
        Topic = topic;
        _title = Topic.TopicEntry.Title;
        Scripts = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Cast<ScriptItem>());
        short associatedScriptIndex = Topic.TopicEntry.Type == TopicType.Main ? Topic.HiddenMainTopic?.EventIndex ?? Topic.TopicEntry.EventIndex : Topic.TopicEntry.EventIndex;
        _associatedScript = (ScriptItem)window.OpenProject.Items.FirstOrDefault(i =>
            i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == associatedScriptIndex);
        EpisodeGroups = [Strings.EPISODE_1, Strings.EPISODE_2, Strings.EPISODE_3, Strings.EPISODE_4, Strings.EPISODE_5];
        _episodeGroup = Topic.TopicEntry.EpisodeGroup;
        _puzzlePhaseGroup = Topic.TopicEntry.PuzzlePhaseGroup;
        _baseTimeGain = Topic.TopicEntry.BaseTimeGain;
        _kyonTimePercentage = Topic.TopicEntry.KyonTimePercentage;
        _mikuruTimePercentage = Topic.TopicEntry.MikuruTimePercentage;
        _nagatoTimePercentage = Topic.TopicEntry.NagatoTimePercentage;
        _koizumiTimePercentage = Topic.TopicEntry.KoizumiTimePercentage;
        KyonTime = BaseTimeGain * _kyonTimePercentage / 100.0;
        MikuruTime = BaseTimeGain * _mikuruTimePercentage / 100.0;
        NagatoTime = BaseTimeGain * _nagatoTimePercentage / 100.0;
        KoizumiTime = BaseTimeGain * _koizumiTimePercentage / 100.0;

        this.WhenAnyValue(t => t.AssociatedScript).ObserveWithHistory(s => AssociatedScript = s, AssociatedScript, _history);
        this.WhenAnyValue(t => t.EpisodeGroup).ObserveWithHistory(g => EpisodeGroup = g, EpisodeGroup, _history);
        this.WhenAnyValue(t => t.PuzzlePhaseGroup).ObserveWithHistory(g => PuzzlePhaseGroup = g, PuzzlePhaseGroup, _history);
        this.WhenAnyValue(t => t.BaseTimeGain).ObserveWithHistory(b => BaseTimeGain = b, BaseTimeGain, _history);
        this.WhenAnyValue(t => t.KyonTimePercentage).ObserveWithHistory(k => KyonTimePercentage = k, KyonTimePercentage, _history);
        this.WhenAnyValue(t => t.MikuruTimePercentage).ObserveWithHistory(m => MikuruTimePercentage = m, MikuruTimePercentage, _history);
        this.WhenAnyValue(t => t.NagatoTimePercentage).ObserveWithHistory(n => NagatoTimePercentage = n, NagatoTimePercentage, _history);
        this.WhenAnyValue(t => t.KoizumiTimePercentage).ObserveWithHistory(k => KoizumiTimePercentage = k, KoizumiTimePercentage, _history);

        UndoCommand = ReactiveCommand.Create(() => _history.Undo());
        RedoCommand = ReactiveCommand.Create(() => _history.Redo());
        UndoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Z);
        RedoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Y);
    }
}
