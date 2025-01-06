using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors;

public class GroupSelectionEditorViewModel : EditorViewModel
{
    [Reactive]
    public GroupSelectionItem GroupSelection { get; set; }
    [Reactive]
    public ObservableCollection<ScenarioActivityViewModel> Activities { get; set; }
    public Project OpenProject { get; set; }
    public EditorTabsPanelViewModel Tabs { get; }

    public GroupSelectionEditorViewModel(GroupSelectionItem groupSelection, MainWindowViewModel window, ILogger log) : base(groupSelection, window, log)
    {
        Tabs = window.EditorTabs;
        GroupSelection = groupSelection;
        OpenProject = window.OpenProject;
        Activities = new(groupSelection.Selection.Activities.Where(a => a is not null).Select((a, i) =>
            new ScenarioActivityViewModel(this, a, i)));
    }
}

public class ScenarioActivityViewModel(GroupSelectionEditorViewModel selection, ScenarioActivity activity, int index) : ViewModelBase
{
    private GroupSelectionEditorViewModel _selection = selection;

    [Reactive]
    public ScenarioActivity Activity { get; set; } = activity;

    private int _index = index;

    public int Index
    {
        get => _index;
        set
        {
            this.RaiseAndSetIfChanged(ref _index, value);
            BackgroundColor = GetBackgroundColor(_index);
            Rect canvasPos = GetCanvasPos(_index);
            CanvasLeft = canvasPos.Left;
            CanvasTop = canvasPos.Top;
            CanvasWidth = canvasPos.Width;
            CanvasHeight = canvasPos.Height;
        }
    }

    private string _title = activity.Title;
    public string Title
    {
        get => _title.GetSubstitutedString(selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value.GetOriginalString(selection.OpenProject));
            Activity.Title = _title;
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    private string _futureDesc = activity.FutureDesc;
    public string FutureDesc
    {
        get => _futureDesc.GetSubstitutedString(selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _futureDesc, value.GetOriginalString(_selection.OpenProject));
            Activity.FutureDesc = _futureDesc;
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    private string _pastDesc = activity.PastDesc;
    public string PastDesc
    {
        get => _pastDesc.GetSubstitutedString(_selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _pastDesc, value.GetSubstitutedString(_selection.OpenProject));
            Activity.PastDesc = _pastDesc;
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    [Reactive]
    public ObservableCollection<ScenarioRouteViewModel> Routes { get; set; } = new(activity.Routes.Select(r => new ScenarioRouteViewModel(selection, r)));

    // Drawing properties
    [Reactive]
    public SolidColorBrush BackgroundColor { get; private set; } = GetBackgroundColor(index);

    [Reactive]
    public double CanvasLeft { get; private set; } = GetCanvasPos(index).Left;
    [Reactive]
    public double CanvasTop { get; private set; } = GetCanvasPos(index).Top;
    [Reactive]
    public double CanvasWidth { get; private set; } = GetCanvasPos(index).Width;
    [Reactive]
    public double CanvasHeight { get; private set; } = GetCanvasPos(index).Height;

    private static Rect GetCanvasPos(int index)
    {
        return index switch
        {
            0 => new(33, 1, 110, 74),
            1 => new(33, 77, 110, 74),
            2 => new(145, 1, 110, 74),
            3 => new(145, 77, 110, 74),
            _ => new(0, 0, 0, 0),
        };
    }

    private static SolidColorBrush GetBackgroundColor(int index)
    {
        return index switch
        {
            0 => new(new Color(255, 191, 95, 95)),
            1 => new(new Color(255, 97, 97, 195)),
            2 => new(new Color(255, 85, 195, 85)),
            3 => new(new Color(255, 195, 166, 52)),
            _ => new(new Color(255, 52, 52, 52)),
        };
    }
}

public class ScenarioRouteViewModel(GroupSelectionEditorViewModel selection, ScenarioRoute route) : ViewModelBase
{
    private GroupSelectionEditorViewModel _selection = selection;

    [Reactive]
    public ScenarioRoute Route { get; set; } = route;

    private string _title = route.Title;
    public string Title
    {
        get => _title.GetSubstitutedString(_selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value.GetOriginalString(_selection.OpenProject));
            Route.Title = _title;
            // _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    public ObservableCollection<TopicItem> KyonlessTopics { get; set; } = new(route.KyonlessTopics.Select(t =>
            (TopicItem)selection.OpenProject.Items.FirstOrDefault(i =>
                i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).TopicEntry.Id == t))
        .Where(t => t is not null));

    private ScriptItem _script = (ScriptItem)selection.OpenProject.Items.FirstOrDefault(i =>
        i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == route.ScriptIndex);
    public ScriptItem Script
    {
        get => _script;
        set
        {
            this.RaiseAndSetIfChanged(ref _script, value);
            Route.ScriptIndex = (short)_script.Event.Index;
            // selection.GroupSelection.UnsavedChanges = true;
        }
    }
}
