using HaruhiChokuretsuLib.Archive.Event;
using System.Linq;

namespace SerialLoops.Lib.Script.Parameters
{
    public class ScriptSectionScriptParameter : ScriptParameter
    {
        public ScriptSection Section { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { ((EventFile)obj).LabelsSection.Objects.First(l => l.Name == Section.Name).Id };

        public ScriptSectionScriptParameter(string name, ScriptSection section) : base(name, ParameterType.SCRIPT_SECTION)
        {
            Section = section;
        }

        public override ScriptSectionScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Section);
        }
    }
}
