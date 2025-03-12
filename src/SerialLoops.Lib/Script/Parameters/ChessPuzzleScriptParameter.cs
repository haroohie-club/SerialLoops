using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class ChessPuzzleScriptParameter : ScriptParameter
{
    public ChessPuzzleItem ChessPuzzle { get; set; }
    public override short[] GetValues(object obj = null) => [(short)ChessPuzzle.ChessPuzzle.Index];

    public override string GetValueString(Project project)
    {
        return ChessPuzzle.DisplayName;
    }

    public ChessPuzzleScriptParameter(string name, ChessPuzzleItem chessPuzzle) : base(name, ParameterType.CHESS_FILE)
    {
        ChessPuzzle =  chessPuzzle;
    }

    public override ChessPuzzleScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ChessPuzzle);
    }
}
