using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Script
{
    public abstract class ScriptParameter
    {
        public ParameterType Type { get; protected set; }
        public string Name { get; protected set; }

        protected ScriptParameter(string name, ParameterType type)
        {
            Name = name;
            Type = type;
        }
        
        public enum ParameterType
        {
            SHORT,
            STRING,
            BOOL
        }

    }
}
