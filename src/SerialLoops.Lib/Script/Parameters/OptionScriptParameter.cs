using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class OptionScriptParameter : ScriptParameter
    {
        public ChoicesSectionEntry Option { get; set; }

        public OptionScriptParameter(string name, ChoicesSectionEntry option) : base(name, ParameterType.OPTION)
        {
            Option = option;
        }
    }
}
