using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : ViewModelBase
{
    public ScriptItemCommand Command { get; set; } = command;
    public ScriptEditorViewModel ScriptEditor { get; set; } = scriptEditor;
    public ScriptItem Script { get; set; } = (ScriptItem)scriptEditor.Description;
    public ILogger Log { get; set; } = log;
    public short MaxShort { get; } = short.MaxValue;
    public short MinShort { get; } = short.MinValue;
}
