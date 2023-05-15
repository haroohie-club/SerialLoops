﻿using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class BgScriptParameter : ScriptParameter
    {
        public BackgroundItem Background { get; set; }
        public bool Kinetic { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)Background.Id };

        public BgScriptParameter(string name, BackgroundItem background, bool kinetic) : base(name, ParameterType.BG)
        {
            Background = background;
            Kinetic = kinetic;
        }

        public override BgScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Background, Kinetic);
        }
    }
}
