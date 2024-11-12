using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class OptionScriptParameter(string name, ChoicesSectionEntry option)
    : ScriptParameter(name, ParameterType.OPTION)
{
    public ChoicesSectionEntry Option { get; set; } = option;

    public override short[] GetValues(object? obj = null) => [(short)((EventFile)obj!).ChoicesSection.Objects.IndexOf(Option)
    ];

    public override OptionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Option);
    }
}
