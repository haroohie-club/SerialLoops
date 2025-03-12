using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
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
    [Reactive]
    public ScenarioActivityViewModel SelectedActivity { get; set; }
    public Project OpenProject { get; }
    public EditorTabsPanelViewModel Tabs { get; }
    public Dictionary<Speaker, SKBitmap> CharacterPortraits { get; } = [];

    public GroupSelectionEditorViewModel(GroupSelectionItem groupSelection, MainWindowViewModel window, ILogger log) : base(groupSelection, window, log)
    {
        Tabs = window.EditorTabs;
        GroupSelection = groupSelection;
        OpenProject = window.OpenProject;

        SKBitmap characterPortraitImage = OpenProject.Grp.GetFileByIndex(0xBAA).GetImage(transparentIndex: 0);
        foreach (Speaker speaker in new[] { Speaker.KYON, Speaker.HARUHI, Speaker.MIKURU, Speaker.NAGATO, Speaker.KOIZUMI })
        {
            int yOffset = (((int)speaker - 1) / 4) * 32;
            int xOffset = (((int)speaker - 1) % 4) * 32;

            SKBitmap characterPortrait = new(32, 32);
            characterPortraitImage.ExtractSubset(characterPortrait, new(xOffset, yOffset, xOffset + 32, yOffset + 32));

            CharacterPortraits.Add(speaker, characterPortrait);
        }
        SKBitmap anyPortrait = new(24, 24);
        characterPortraitImage.ExtractSubset(anyPortrait, new(96, 96, 120, 120));
        CharacterPortraits.Add(Speaker.INFO, anyPortrait);

        // We don't do the Where in advance because we need the index to be accurate
        Activities = new(groupSelection.Selection.Activities.Select((a, i) => a is not null ? new ScenarioActivityViewModel(this, a, i) : null)
            .Where(a => a is not null));
    }
}

public class ScenarioActivityViewModel : ViewModelBase
{
    private GroupSelectionEditorViewModel _selection;

    [Reactive]
    public bool Selected { get; set; }

    [Reactive]
    public ScenarioActivity Activity { get; set; }

    public ICommand SelectActivityCommand { get; }
    public ICommand SelectFutureDescCommand { get; }
    public ICommand SelectPastDescCommand { get; }

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
            SetTitleTextImage();
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
            SetSelectedDescriptionImage(_futureDesc);
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    private string _pastDesc;
    public string PastDesc
    {
        get => _pastDesc.GetSubstitutedString(_selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _pastDesc, value.GetOriginalString(_selection.OpenProject));
            Activity.PastDesc = _pastDesc;
            SetSelectedDescriptionImage(_pastDesc);
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    [Reactive]
    public SKBitmap SelectedDescriptionImage { get; set; }

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

        TitlePlateSlope = new(16, 16);
        TitlePlateMain = new(16, 32);
        _layoutSource.ExtractSubset(TitlePlateSlope, new(32, 48, 48, 64));
        _layoutSource.ExtractSubset(TitlePlateMain, new(48, 32, 64, 64));

        SetTitleTextImage();

        SelectActivityCommand = ReactiveCommand.Create(() =>
        {
            if (_selection.SelectedActivity is not null)
            {
                _selection.SelectedActivity.Selected = false;
            }
            Selected = true;
            SelectedDescriptionImage = null;
            _selection.SelectedActivity = this;
        });
        SelectFutureDescCommand = ReactiveCommand.Create(() => SetSelectedDescriptionImage(_futureDesc));
        SelectPastDescCommand = ReactiveCommand.Create(() => SetSelectedDescriptionImage(_pastDesc));

        LockedIcons = [];
        if (Activity.RequiredBrigadeMember != ScenarioActivity.BrigadeMember.NONE)
        {
            LockedIcons.Add(Activity.RequiredBrigadeMember switch
            {
                ScenarioActivity.BrigadeMember.MIKURU => _selection.CharacterPortraits[Speaker.MIKURU].Resize(new SKSizeI(48, 48), SKSamplingOptions.Default),
                ScenarioActivity.BrigadeMember.NAGATO => _selection.CharacterPortraits[Speaker.NAGATO].Resize(new SKSizeI(48, 48), SKSamplingOptions.Default),
                ScenarioActivity.BrigadeMember.KOIZUMI => _selection.CharacterPortraits[Speaker.KOIZUMI].Resize(new SKSizeI(48, 48), SKSamplingOptions.Default),
                ScenarioActivity.BrigadeMember.ANY => _selection.CharacterPortraits[Speaker.INFO].Resize(new SKSizeI(48, 48), SKSamplingOptions.Default),
                _ => null,
            });
        }
        if (Activity.HaruhiPresent)
        {
            LockedIcons.Add(_selection.CharacterPortraits[Speaker.HARUHI].Resize(new SKSizeI(24, 24), SKSamplingOptions.Default));
        }
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

    public SKBitmap TitlePlateSlope { get; }
    public SKBitmap TitlePlateMain { get; }
    [Reactive]
    public SKBitmap TitlePlateText { get; set; }

    public ObservableCollection<SKBitmap> LockedIcons { get; }

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

    private void SetTitleTextImage()
    {
        TitlePlateText = new(68, 16);
        using SKCanvas titlePlateCanvas = new(TitlePlateText);
        titlePlateCanvas.DrawHaroohieText(Activity.Title, DialogueScriptParameter.Paint00, _selection.OpenProject, 0, 0);
        titlePlateCanvas.Flush();
        TitlePlateText = TitlePlateText.Resize(new SKSizeI(136, 32), SKSamplingOptions.Default);
    }

    private void SetSelectedDescriptionImage(string description)
    {
        SelectedDescriptionImage = new(256, 40);
        using SKCanvas canvas = new(SelectedDescriptionImage);
        canvas.DrawBitmap(_selection.OpenProject.DialogueBitmap, new(0, 24, 32, 36), new SKRect(0, 0, 256, 12));
        SKColor dialogueBoxColor = _selection.OpenProject.DialogueBitmap.GetPixel(0, 28);
        canvas.DrawRect(0, 12, 256, 28, new() { Color = dialogueBoxColor });
        canvas.DrawBitmap(_selection.OpenProject.DialogueBitmap, new(0, 37, 32, 64),
            new SKRect(224, 13, 256, 40));
        canvas.DrawHaroohieText(description, DialogueScriptParameter.Paint00, _selection.OpenProject, y: 8);
        canvas.Flush();
        SelectedDescriptionImage = SelectedDescriptionImage.Resize(new SKSizeI(512, 80), SKSamplingOptions.Default);
    }
}

public class ScenarioRouteViewModel : ViewModelBase
{
    private GroupSelectionEditorViewModel _selection;

    public EditorTabsPanelViewModel Tabs => _selection.Tabs;

    [Reactive]
    public ScenarioRoute Route { get; set; }

    private string _title;
    public string Title
    {
        get => _title.GetSubstitutedString(_selection.OpenProject);
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value.GetOriginalString(_selection.OpenProject));
            Route.Title = _title;
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    public ObservableCollection<TopicItem> KyonlessTopics { get; set; }

    private ScriptItem _script;
    public ScriptItem Script
    {
        get => _script;
        set
        {
            this.RaiseAndSetIfChanged(ref _script, value);
            Route.ScriptIndex = (short)_script.Event.Index;
            _selection.GroupSelection.UnsavedChanges = true;
        }
    }

    public ScenarioRouteViewModel(GroupSelectionEditorViewModel selection, ScenarioRoute route)
    {
        _selection = selection;
        Route = route;
        _title = route.Title;
        KyonlessTopics = new(route.KyonlessTopics.Select(t =>
                (TopicItem)selection.OpenProject.Items.FirstOrDefault(i =>
                    i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).TopicEntry.Id == t))
            .Where(t => t is not null));
        _script = (ScriptItem)selection.OpenProject.Items.FirstOrDefault(i =>
            i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == route.ScriptIndex);

        CharacterIcons = new(Route.CharactersInvolved.Select(s => _selection.CharacterPortraits[s]));
    }

    public ObservableCollection<SKBitmap> CharacterIcons { get; }
}
