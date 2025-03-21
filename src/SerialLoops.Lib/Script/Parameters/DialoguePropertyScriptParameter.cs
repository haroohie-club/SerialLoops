﻿using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class DialoguePropertyScriptParameter : ScriptParameter
{
    public CharacterItem Character { get; set; }
    public override short[] GetValues(object obj = null) => [(short)((MessageInfoFile)obj).MessageInfos.FindIndex(m => m.Character == Character.MessageInfo.Character),
    ];

    public override string GetValueString(Project project)
    {
        return Character.DisplayName;
    }

    public DialoguePropertyScriptParameter(string name, CharacterItem character) : base(name, ParameterType.CHARACTER)
    {
        Character = character;
    }

    public override DialoguePropertyScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Character);
    }
}
