using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Script.Parameters;

public class DialogueScriptParameter : ScriptParameter
{
    public DialogueLine Line { get; set; }
    public override short[] GetValues(object obj = null) => [(short)((EventFile)obj).DialogueSection.Objects.FindIndex(l => l == Line),
    ];

    public override string GetValueString(Project project)
    {
        return $"{project.GetCharacterBySpeaker(Line.Speaker).DisplayName}: {Line.Text}";
    }

    public DialogueScriptParameter(string name, DialogueLine line) : base(name, ParameterType.DIALOGUE)
    {
        Line = line;
    }

    public override DialogueScriptParameter Clone(Project project, EventFile eventFile)
    {
        DialogueLine line = new(Line.Text.GetOriginalString(project), eventFile) { Speaker = Line.Speaker };
        eventFile.DialogueSection.Objects.Add(line);
        return new(Name, line);
    }
}
