using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors;

public class TopicEditorViewModel : EditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    [Reactive]
    public TopicItem Topic { get; set; }

    private string _title;

    public string Title
    {
        get => _title.GetSubstitutedString(Window.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value.GetOriginalString(Window.OpenProject));
            Topic.TopicEntry.Title = _title;
            Topic.DisplayName = $"{Topic.TopicEntry.Id} - {_title.GetSubstitutedString(Window.OpenProject)}";
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

    public short MaxShort => short.MaxValue;

    public TopicEditorViewModel(TopicItem topic, MainWindowViewModel window, ILogger log) : base(topic, window, log)
    {
        Tabs = window.EditorTabs;
        Topic = topic;
        _title = Topic.TopicEntry.Title;
        Scripts = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Cast<ScriptItem>());
        short associatedScriptIndex = (short)(Topic.TopicEntry.Type == TopicType.Main ? Topic.HiddenMainTopic?.EventIndex ?? Topic.TopicEntry.EventIndex : Topic.TopicEntry.EventIndex);
        _associatedScript = (ScriptItem)window.OpenProject.Items.FirstOrDefault(i =>
            i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == associatedScriptIndex);
        EpisodeGroups = [Strings.Episode_1, Strings.Episode_2, Strings.Episode_3, Strings.Episode_4, Strings.Episode_5];
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
    }
}
