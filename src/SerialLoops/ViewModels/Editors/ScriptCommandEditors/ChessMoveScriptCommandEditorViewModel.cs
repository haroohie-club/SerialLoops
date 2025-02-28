using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Script;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChessMoveScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{

}
