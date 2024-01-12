using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class PuzzleItem : Item
    {
        public PuzzleFile Puzzle { get; set; }
        public SKBitmap SingularityImage { get; set; }

        public PuzzleItem(PuzzleFile puzzleFile, Project project, ILogger log) : base(puzzleFile.Name[0..^1], ItemType.Puzzle)
        {
            Puzzle = puzzleFile;
            Refresh(project, log);
        }

        public override void Refresh(Project project, ILogger log)
        {
            GraphicsFile singularityLayout = project.Grp.Files.First(f => f.Index == Puzzle.Settings.SingularityLayout);
            GraphicsFile singularityTexture = project.Grp.Files.First(f => f.Index == Puzzle.Settings.SingularityTexture);
            SingularityImage = singularityLayout.GetLayout(
                    [singularityTexture],
                    0,
                    singularityLayout.LayoutEntries.Count,
                    false,
                    true).bitmap;
        }

        public static readonly List<string> Characters =
        [
            "UNKNOWN (0)",
            "KYON",
            "HARUHI",
            "MIKURU",
            "NAGATO",
            "KOIZUMI",
            "ANY",
        ];
    }
}
