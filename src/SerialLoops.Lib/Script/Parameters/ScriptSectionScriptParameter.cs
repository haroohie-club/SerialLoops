using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class ScriptSectionScriptParameter : ScriptParameter
    {
        public ScriptSection Section { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)(Section is not null ? ((EventFile)obj).LabelsSection.Objects.First(l => l.Name.Replace("/", "") == Section.Name).Id : 0) };

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
