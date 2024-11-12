using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ConditionalScriptParameter(string name, string value) : ScriptParameter(name, ParameterType.CONDITIONAL)
{
    public string Conditional { get; set; } = value;

    public override short[] GetValues(object? obj = null) => [(short)((EventFile)obj!).ConditionalsSection.Objects.FindIndex(c => c.Equals(Conditional))
    ];

    public override ConditionalScriptParameter Clone(Project project, EventFile eventFile)
    {
        var newIndex = eventFile.ConditionalsSection.Objects.Count;
        eventFile.ConditionalsSection.Objects.Add(Conditional);
        return new(Name, eventFile.ConditionalsSection.Objects[newIndex]);
    }
}
