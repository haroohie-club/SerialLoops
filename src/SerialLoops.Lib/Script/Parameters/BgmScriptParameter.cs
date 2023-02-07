using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Script.Parameters
{
    public class BgmScriptParameter : ScriptParameter
    {
        public BackgroundMusicItem Bgm { get; set; }

        public BgmScriptParameter(string name, BackgroundMusicItem bgm) : base(name, ParameterType.BGM)
        {
            Bgm = bgm;
        }
    }
}
