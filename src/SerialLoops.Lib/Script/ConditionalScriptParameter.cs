﻿namespace SerialLoops.Lib.Script
{
    public class ConditionalScriptParameter : ScriptParameter
    {
        public string Value { get; set; }

        public ConditionalScriptParameter(string name, string value) : base(name, ParameterType.CONDITIONAL)
        {
            Value = value;
        }
    }
}
