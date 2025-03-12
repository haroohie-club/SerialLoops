using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Script;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class EmptyScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
}
