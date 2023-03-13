using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class ScriptSectionScriptParameter : ScriptParameter
    {
        public ScriptSection Section { get; set; }

        public ScriptSectionScriptParameter(string name, ScriptSection section) : base(name, ParameterType.SCRIPT_SECTION)
        {
            Section = section;
        }

        public override ScriptSectionScriptParameter Clone()
        {
            return new(Name, Section);
        }
    }
}
