using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Script
{
    public class BoolScriptParameter : ScriptParameter
    {
        public bool Value { get; set; }

        public BoolScriptParameter(string name, bool value) : base(name, ParameterType.BOOL)
        {
            Value = value;
        }
  
       
    }
}
