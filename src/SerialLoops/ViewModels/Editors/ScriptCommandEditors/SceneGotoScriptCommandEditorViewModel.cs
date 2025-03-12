using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class SceneGotoScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<ScriptItem> Scripts { get; }
    private ScriptItem _selectedScript;

    public ScriptItem SelectedScript
    {
        get => _selectedScript;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedScript, value);
            ((ConditionalScriptParameter)Command.Parameters[0]).Conditional = _selectedScript.Name;
            if (Script.Event.ConditionalsSection.Objects.Contains(_selectedScript.Name))
            {
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[0] =
                    (short)Script.Event.ConditionalsSection.Objects.IndexOf(_selectedScript.Name);
            }
            else
            {
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[0] =
                    (short)(Script.Event.ConditionalsSection.Objects.Count - 1);
                Script.Event.ConditionalsSection.Objects.Insert(Script.Event.ConditionalsSection.Objects.Count - 1, _selectedScript.Name);
            }
            Script.UnsavedChanges = true;
        }
    }

    public SceneGotoScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) :
        base(command, scriptEditor, log)
    {
        Tabs = window.EditorTabs;
        Scripts = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Script)
            .Cast<ScriptItem>());
        _selectedScript = (ScriptItem)(window.OpenProject.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Name.Equals(((ConditionalScriptParameter)Command.Parameters[0]).Conditional))
            ?? window.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Script));
    }
}
