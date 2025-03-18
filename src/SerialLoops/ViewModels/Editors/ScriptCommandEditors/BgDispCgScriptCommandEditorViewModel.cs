using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class BgDispCgScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private MainWindowViewModel _window;

    private BackgroundItem _cg;
    public BackgroundItem Cg
    {
        get => _cg;
        set
        {
            this.RaiseAndSetIfChanged(ref _cg, value);
            ((BgScriptParameter)Command.Parameters[0]).Background = _cg;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short?)_cg?.Id ?? 0;
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public bool DisplayFromBottom
    {
        get => ((BoolScriptParameter)Command.Parameters[1]).Value;
        set
        {
            ((BoolScriptParameter)Command.Parameters[1]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = value ? ((BoolScriptParameter)Command.Parameters[1]).TrueValue : ((BoolScriptParameter)Command.Parameters[1]).FalseValue;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public EditorTabsPanelViewModel Tabs { get; }
    public ICommand ReplaceCgCommand { get; }

    public BgDispCgScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        _window = ScriptEditor.Window;
        _cg = ((BgScriptParameter)Command.Parameters[0]).Background;
        Tabs = scriptEditor.Window.EditorTabs;

        ReplaceCgCommand = ReactiveCommand.CreateFromTask(ReplaceCg);
    }

    private async Task ReplaceCg()
    {
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(new List<IPreviewableGraphic> { NonePreviewableGraphic.BACKGROUND }.Concat(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Background).Cast<IPreviewableGraphic>()),
            Cg, _window.OpenProject, _window.Log, i => i.Name == "NONE" ||
                                                       (((BackgroundItem)i).BackgroundType != BgType.TEX_BG && ((BackgroundItem)i).BackgroundType != BgType.KINETIC_SCREEN));
        IPreviewableGraphic cg = await new GraphicSelectionDialog { DataContext = graphicSelectionDialog }.ShowDialog<IPreviewableGraphic>(_window.Window);
        if (cg is null)
        {
            return;
        }
        else if (cg.Text == "NONE")
        {
            Cg = null;
        }
        else
        {
            Cg = (BackgroundItem)cg;
        }
    }
}
