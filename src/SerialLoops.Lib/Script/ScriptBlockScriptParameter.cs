using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script
{
    public class ScriptSectionScriptParameter : ScriptParameter
    {
        public ScriptSectionDefinition Section { get; set; }

        public ScriptSectionScriptParameter(string name, ScriptSectionDefinition section) : base(name, ParameterType.SCRIPT_SECTION)
        {
            Section = section;
        }
    }
}
