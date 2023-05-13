using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System.Collections.Generic;

namespace SerialLoops.Lib.Items
{
    public class CharacterItem : Item
    {
        public MessageInfo MessageInfo { get; set; }
        public CharacterInfo CharacterInfo { get; set; }

        public CharacterItem(MessageInfo character, CharacterInfo characterInfo, Project project) : base($"CHR_{project.Characters[(int)character.Character].Name}", ItemType.Character)
        {
            MessageInfo = character;
            CharacterInfo = characterInfo;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }

        public static readonly Dictionary<string, SKColor> BuiltInColors = new()
        {
            { "Kyon", new SKColor(240, 240, 0) },
            { "Haruhi", new SKColor(240, 0, 104) },
            { "Asahina", new SKColor(240, 112, 0) },
            { "Nagato", new SKColor(0, 192, 240) },
            { "Koizumi", new SKColor(172, 96, 248) },
            { "Sister", new SKColor(224, 0, 240) },
            { "Tsuruya", new SKColor(0, 192, 0) },
            { "Taniguchi", new SKColor(200, 104, 104) },
            { "Kunikida", new SKColor(200, 104, 0) },
            { "President", new SKColor(192, 192, 0) },
            { "Other", new SKColor(80, 80, 80) },
            { "Other 2", new SKColor(112, 112, 112) },
            { "Okabe", new SKColor(144, 88, 80) },
            { "Girl", new SKColor(48, 240, 152) },
            { "No Nameplate", new SKColor(0, 0, 0, 0) },
            { "Email", new SKColor(0, 240, 144) },
        };
    }

    public struct CharacterInfo
    {
        public string Name { get; set; }
        public SKColor NameColor { get; set; }
        public SKColor PlateColor { get; set; }

        public CharacterInfo(string name, SKColor nameColor, SKColor plateColor)
        {
            Name = name;
            NameColor = nameColor;
            PlateColor = plateColor;
        }
    }
}
