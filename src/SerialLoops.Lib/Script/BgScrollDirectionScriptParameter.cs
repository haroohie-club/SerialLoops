using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Script
{
    public class BgScrollDirectionScriptParameter : ScriptParameter
    {
        public BgScrollDirection ScrollDirection { get; set; }

        public BgScrollDirectionScriptParameter(string name, short scrollDirection) : base(name, ParameterType.BG_SCROLL_DIRECTION)
        {
            ScrollDirection = (BgScrollDirection)scrollDirection;
        }

        public enum BgScrollDirection : short
        {
            DOWN = 1,
            UP = 2,
            LEFT = 3,
            RIGHT = 4,
            DIAGONAL_RIGHT_DOWN = 5,
            DIAGONAL_LEFT_UP = 6,
        }
    }
}
