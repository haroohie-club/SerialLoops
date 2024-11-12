using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ScriptSectionScriptParameter(string name, ScriptSection? section)
    : ScriptParameter(name, ParameterType.SCRIPT_SECTION)
{
    public ScriptSection? Section { get; set; } = section;

    public override short[] GetValues(object? obj = null) => [(short)(Section is not null ? ((EventFile)obj!).LabelsSection.Objects.First(l => l.Name.Replace("/", "") == Section.Name).Id : 0)
    ];

    public override ScriptSectionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Section);
    }
}
