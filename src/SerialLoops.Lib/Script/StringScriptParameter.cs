using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Script
{
    public class StringScriptParameter : ScriptParameter
    {
        public string Value { get; set; }

        public StringScriptParameter(string name, string value) : base(name, ParameterType.STRING)
        {
            Value = value;
        }
        
    }
}
