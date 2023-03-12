using HaruhiChokuretsuLib.Archive.Event;
using SkiaSharp;

namespace SerialLoops.Lib.Script.Parameters
{
    public class DialogueScriptParameter : ScriptParameter
    {
        public DialogueLine Line { get; set; }

        public DialogueScriptParameter(string name, DialogueLine line) : base(name, ParameterType.DIALOGUE)
        {
            Line = line;
        }

        public override DialogueScriptParameter Clone()
        {
            return new(Name, Line); //todo Need to create a new dialogueline with the same text, adjust data file accordingly?
        }

        public static SKPaint Paint00 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                1.00f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 1.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 1.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint Paint01 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.69f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.69f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.38f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint Paint02 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.96f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.96f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.96f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint Paint03 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.65f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.67f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.75f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint Paint04 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.55f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.48f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.65f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint Paint05 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.75f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.14f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.23f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint Paint06 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.36f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.36f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.36f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint Paint07 { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.00f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
    }
}
