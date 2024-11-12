using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Script.Parameters;

public class DialogueScriptParameter(string name, DialogueLine line) : ScriptParameter(name, ParameterType.DIALOGUE)
{
    public DialogueLine Line { get; set; } = line;

    public override short[] GetValues(object? obj = null) => [(short)((EventFile)obj!).DialogueSection.Objects.FindIndex(l => l == Line)
    ];

    public override DialogueScriptParameter Clone(Project project, EventFile eventFile)
    {
        DialogueLine line = new(Line.Text.GetOriginalString(project), eventFile);
        eventFile.DialogueSection.Objects.Insert(eventFile.DialogueSection.Objects.Count - 1, line);
        return new(Name, line);
    }

    public static SKPaint Paint00 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            1.00f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 1.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 1.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
    public static SKPaint Paint01 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            0.69f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.69f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.38f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
    public static SKPaint Paint02 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            0.96f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.96f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.96f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
    public static SKPaint Paint03 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            0.65f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.67f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.75f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
    public static SKPaint Paint04 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            0.55f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.48f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.65f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
    public static SKPaint Paint05 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            0.75f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.14f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.23f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
    public static SKPaint Paint06 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            0.36f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.36f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.36f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
    public static SKPaint Paint07 { get; } = new()
    {
        ColorFilter = SKColorFilter.CreateColorMatrix([
            0.00f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 0.00f, 0.00f,
            0.00f, 0.00f, 0.00f, 1.00f, 0.00f
        ]),
    };
}
