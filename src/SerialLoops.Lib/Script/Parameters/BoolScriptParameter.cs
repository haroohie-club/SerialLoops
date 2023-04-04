﻿using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class BoolScriptParameter : ScriptParameter
    {
        public bool Value { get; set; }

        public BoolScriptParameter(string name, bool value) : base(name, ParameterType.BOOL)
        {
            Value = value;
        }

        public override BoolScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Value);
        }
    }
}
