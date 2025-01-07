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
using SkiaSharp;

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
        // We don't do the Where in advance because we need the index to be accurate
        Activities = new(groupSelection.Selection.Activities.Select((a, i) => a is not null ? new ScenarioActivityViewModel(this, a, i) : null)
            .Where(a => a is not null));
    }
}

public class ScenarioActivityViewModel : ViewModelBase
{
    private GroupSelectionEditorViewModel _selection;

    [Reactive]
    public ScenarioActivity Activity { get; set; }

    private int _index;
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

    private string _title;
    public string Title
    {
        get => _title.GetSubstitutedString(_selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value.GetOriginalString(_selection.OpenProject));
            Activity.Title = _title;
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    private string _futureDesc;
    public string FutureDesc
    {
        get => _futureDesc.GetSubstitutedString(_selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _futureDesc, value.GetOriginalString(_selection.OpenProject));
            Activity.FutureDesc = _futureDesc;
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    private string _pastDesc;
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
    public ObservableCollection<ScenarioRouteViewModel> Routes { get; set; }

    public ScenarioActivityViewModel(GroupSelectionEditorViewModel selection, ScenarioActivity activity, int index)
    {
        _selection = selection;
        Activity = activity;
        _index = index;
        _title = activity.Title;
        _futureDesc = Activity.FutureDesc;
        _pastDesc = Activity.PastDesc;
        Routes = new(activity.Routes.Select(r => new ScenarioRouteViewModel(selection, r)));

        _layoutSource = selection.OpenProject.Grp.GetFileByIndex(0xB98).GetImage(transparentIndex: 0);
        BackgroundColor = GetBackgroundColor(index);
        Rect canvasPos = GetCanvasPos(index);
        CanvasLeft = canvasPos.Left;
        CanvasTop = canvasPos.Top;
        CanvasWidth = canvasPos.Width;
        CanvasHeight = canvasPos.Height;
        Letter = GetLetter(index);
    }

    // Drawing properties
    private SKBitmap _layoutSource;

    [Reactive]
    public SolidColorBrush BackgroundColor { get; private set; }

    [Reactive]
    public double CanvasLeft { get; private set; }
    [Reactive]
    public double CanvasTop { get; private set; }
    [Reactive]
    public double CanvasWidth { get; private set; }
    [Reactive]
    public double CanvasHeight { get; private set; }

    [Reactive]
    public SKBitmap Letter { get; private set; }

    private static Rect GetCanvasPos(int index)
    {
        return index switch
        {
            0 => new(33, 1, 220, 148),
            1 => new(33, 151, 220, 148),
            2 => new(256, 1, 220, 148),
            3 => new(256, 151, 220, 148),
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

    private SKBitmap GetLetter(int index)
    {
        SKBitmap letter = new(32, 32);
        _layoutSource.ExtractSubset(letter, index switch
        {
            0 => new(0, 0, 32, 32),
            1 => new(32, 0, 64, 32),
            2 => new(64, 0, 96, 32),
            3 => new(96, 0, 128, 32),
            _ => new(0, 0, 1, 1),
        });
        SKColor tint = index switch
        {
            0 => new(0xD9, 0x80, 0x80, 0xFF),
            1 => new (0x80, 0x80, 0xFF, 0xFF),
            2 => new(0x80, 0xD0, 0x80, 0xFF),
            3 => new(0xD0, 0xC0, 0x40, 0xFF),
            _ => new(255, 255, 255, 255),
        };
        for (int x = 0; x < letter.Width; x++)
        {
            for (int y = 0; y < letter.Height; y++)
            {
                SKColor color = letter.GetPixel(x, y);
                letter.SetPixel(x, y, new((byte)(color.Red * tint.Red / (double)byte.MaxValue),
                    (byte)(color.Green * tint.Green / (double)byte.MaxValue),
                    (byte)(color.Blue * tint.Blue / (double)byte.MaxValue),
                    color.Alpha));
            }
        }

        return letter;
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
