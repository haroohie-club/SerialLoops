using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class OptionScriptParameter : ScriptParameter
{
    public ChoicesSectionEntry Option { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)((EventFile)obj).ChoicesSection.Objects.IndexOf(Option) };

    public override string GetValueString(Project project)
    {
        return Option.Text;
    }

    public OptionScriptParameter(string name, ChoicesSectionEntry option) : base(name, ParameterType.OPTION)
    {
        Option = option;
    }

    public override OptionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Option);
    }
}
