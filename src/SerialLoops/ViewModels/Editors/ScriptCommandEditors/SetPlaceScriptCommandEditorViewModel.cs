using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class SetPlaceScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private MainWindowViewModel _window;

    public EditorTabsPanelViewModel Tabs { get; }

    private bool _display;
    public bool Display
    {
        get => _display;
        set
        {
            this.RaiseAndSetIfChanged(ref _display, value);
            BoolScriptParameter parameter = (BoolScriptParameter)Command.Parameters[0];
            parameter.Value = _display;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _display ? parameter.TrueValue : parameter.FalseValue;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    private PlaceItem _place;
    public PlaceItem Place
    {
        get => _place;
        set
        {
            this.RaiseAndSetIfChanged(ref _place, value);
            ((PlaceScriptParameter)Command.Parameters[1]).Place = _place;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short?)_place?.Index ?? 0;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ICommand ChangePlaceCommand { get; }

    public SetPlaceScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window)
        : base(command, scriptEditor, log)
    {
        _window = window;
        Tabs = _window.EditorTabs;
        ChangePlaceCommand = ReactiveCommand.CreateFromTask(ChangePlace);

        _display = ((BoolScriptParameter)Command.Parameters[0]).Value;
        _place = ((PlaceScriptParameter)Command.Parameters[1]).Place;
    }

    private async Task ChangePlace()
    {
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(new List<IPreviewableGraphic> { NonePreviewableGraphic.PLACE }.Concat(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Place).Cast<IPreviewableGraphic>()),
            Place, _window.OpenProject, _window.Log);
        IPreviewableGraphic place = await new GraphicSelectionDialog { DataContext = graphicSelectionDialog }.ShowDialog<IPreviewableGraphic>(_window.Window);
        if (place is null)
        {
            return;
        }
        else if (place.Text == "NONE")
        {
            Place = null;
        }
        else
        {
            Place = (PlaceItem)place;
        }
    }
}
