using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Data;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class KbgDispScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public ICommand ReplaceKbgCommand { get; }
    private MainWindowViewModel _window;
    public EditorTabsPanelViewModel Tabs { get; }

    private BackgroundItem _kbg;
    public BackgroundItem Kbg
    {
        get => _kbg;
        set
        {
            this.RaiseAndSetIfChanged(ref _kbg, value);
            if (_kbg is null)
            {
                ((BgScriptParameter)Command.Parameters[0]).Background = null;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[0] = 0;
            }
            else
            {
                ((BgScriptParameter)Command.Parameters[0]).Background = _kbg;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[0] = (short)_kbg.Id;
            }
        }
    }

    public KbgDispScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, MainWindowViewModel window) : base(command, scriptEditor)
    {
        _kbg = ((BgScriptParameter)command.Parameters[0]).Background;
        ReplaceKbgCommand = ReactiveCommand.CreateFromTask(ReplaceKbg);
        _window = window;
        Tabs = _window.EditorTabs;
    }

    private async Task ReplaceKbg()
    {
        // Order of the predicate matters here as "NONE" short circuits the NonePreviewableGraphic, preventing it from being cast
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(new List<IPreviewableGraphic>() { NonePreviewableGraphic.BACKGROUND }.Concat(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Background).Cast<IPreviewableGraphic>()),
            Kbg, _window.OpenProject, _window.Log, i => i.Name == "NONE" || ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN);
        IPreviewableGraphic bgItem = await new GraphicSelectionDialog() { DataContext = graphicSelectionDialog }.ShowDialog<IPreviewableGraphic>(_window.Window);
        if (bgItem is null || bgItem == Kbg)
        {
            return;
        }
        else if (bgItem.Text == "NONE")
        {
            Kbg = null;
        }
        else
        {
            Kbg = (BackgroundItem)bgItem;
        }
        ScriptEditor.UpdatePreview();
        Script.UnsavedChanges = true;
    }
}