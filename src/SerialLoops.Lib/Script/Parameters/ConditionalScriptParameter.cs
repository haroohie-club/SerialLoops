using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ConditionalScriptParameter : ScriptParameter
{
    public string Conditional { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)((EventFile)obj).ConditionalsSection.Objects.FindIndex(c => c.Equals(Conditional)) };

    public ConditionalScriptParameter(string name, string value) : base(name, ParameterType.CONDITIONAL)
    {
        Conditional = value;
    }

    public override ConditionalScriptParameter Clone(Project project, EventFile eventFile)
    {
        var newIndex = eventFile.ConditionalsSection.Objects.Count;
        eventFile.ConditionalsSection.Objects.Add(Conditional);
        return new(Name, eventFile.ConditionalsSection.Objects[newIndex]);
    }
}