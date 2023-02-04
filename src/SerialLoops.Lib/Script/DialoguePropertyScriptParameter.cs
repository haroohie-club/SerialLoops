﻿using HaruhiChokuretsuLib.Archive.Data;

namespace SerialLoops.Lib.Script
{
    public class DialoguePropertyScriptParameter : ScriptParameter
    {
        public MessageInfo DialogueProperties { get; set; }

        public DialoguePropertyScriptParameter(string name, MessageInfo dialogueProperties) : base(name, ParameterType.DIALOGUE_PROPERTY)
        {
            DialogueProperties = dialogueProperties;
        }
    }
}
