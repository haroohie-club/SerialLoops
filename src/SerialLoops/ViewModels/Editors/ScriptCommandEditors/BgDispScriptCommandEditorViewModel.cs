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

public class BgDispScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private MainWindowViewModel _window;
    public EditorTabsPanelViewModel Tabs { get; }

    private BackgroundItem _bg;
    public BackgroundItem Bg
    {
        get => _bg;
        set
        {
            this.RaiseAndSetIfChanged(ref _bg, value);
            ((BgScriptParameter)Command.Parameters[0]).Background = _bg;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short?)_bg?.Id ?? 0;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ICommand ReplaceBgCommand { get; }

    public BgDispScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) : base(command, scriptEditor, log)
    {
        _window = window;
        Tabs = _window.EditorTabs;
        _bg = ((BgScriptParameter)Command.Parameters[0]).Background;
        ReplaceBgCommand = ReactiveCommand.CreateFromTask(ReplaceBg);
    }

    private async Task ReplaceBg()
    {
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(new List<IPreviewableGraphic> { NonePreviewableGraphic.BACKGROUND }.Concat(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Background).Cast<IPreviewableGraphic>()),
            Bg, _window.OpenProject, _window.Log, i => i.Name == "NONE" || ((BackgroundItem)i).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_BG);
        IPreviewableGraphic bg = await new GraphicSelectionDialog { DataContext = graphicSelectionDialog }.ShowDialog<IPreviewableGraphic>(_window.Window);
        if (bg?.Text == "NONE")
        {
            Bg = null;
        }
        else if (bg is not null)
        {
            Bg = (BackgroundItem)bg;
        }
    }
}
