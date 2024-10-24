using HaruhiChokuretsuLib.Archive.Event;
using System.Collections.Generic;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;

namespace SerialLoops.ViewModels.Editors
{
    public class ScriptCommandEditorViewModel : EditorViewModel
    {
        private ScriptItem _script;
        private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands = [];

        public ScriptCommandEditorViewModel(ScriptItem script, MainWindowViewModel window, ILogger log) : base(script, window, log)
        {
            _script = script;
            PopulateScriptCommands();
            _script.CalculateGraphEdges(_commands, _log);
        }

        public void PopulateScriptCommands(bool refresh = false)
        {
            if (refresh)
            {
                _script.Refresh(_project, _log);
            }
            _commands = _script.GetScriptCommandTree(_project, _log);
        }
    }
}
