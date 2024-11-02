using SerialLoops.Lib.Script;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors
{
    public class ScriptCommandEditorViewModel(ScriptItemCommand command) : ViewModelBase
    {
        public ScriptItemCommand Command { get; set; } = command;
        public short MaxShort { get; } = short.MaxValue;
    }
}
