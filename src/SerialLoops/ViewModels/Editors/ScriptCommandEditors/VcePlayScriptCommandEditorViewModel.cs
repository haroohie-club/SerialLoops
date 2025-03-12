using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class VcePlayScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<VoicedLineItem> Vces { get; }
    private VoicedLineItem _vce;
    public VoicedLineItem Vce
    {
        get => _vce;
        set
        {
            this.RaiseAndSetIfChanged(ref _vce, value);
            ((VoicedLineScriptParameter)Command.Parameters[0]).VoiceLine = _vce;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_vce.Index;
            Script.UnsavedChanges = true;
        }
    }

    public VcePlayScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) :
        base(command, scriptEditor, log)
    {
        Tabs = window.EditorTabs;
        Vces = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Voice).Cast<VoicedLineItem>());
        _vce = ((VoicedLineScriptParameter)Command.Parameters[0]).VoiceLine;
    }
}
