using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class LoadIsomapScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public ObservableCollection<MapItem> Maps { get; } = new(scriptEditor.Window.OpenProject.Items
        .Where(i => i.Type == ItemDescription.ItemType.Map).Cast<MapItem>());
    private MapItem _selectedMap = ((MapScriptParameter)command.Parameters[0]).Map;
    public MapItem SelectedMap
    {
        get => _selectedMap;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMap, value);
            ((MapScriptParameter)Command.Parameters[0]).Map = _selectedMap;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_selectedMap.Map.Index;
            Script.UnsavedChanges = true;
        }
    }
}
