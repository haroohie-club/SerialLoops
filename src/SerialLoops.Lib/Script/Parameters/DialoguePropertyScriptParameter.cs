using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class DialoguePropertyScriptParameter(string name, CharacterItem character)
    : ScriptParameter(name, ParameterType.CHARACTER)
{
    public CharacterItem Character { get; set; } = character;

    public override short[] GetValues(object? obj = null) => [(short)((MessageInfoFile)obj!).MessageInfos.FindIndex(m => m.Character == Character.MessageInfo.Character)
    ];

    public override DialoguePropertyScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Character);
    }
}
