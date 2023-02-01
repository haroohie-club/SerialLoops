using HaruhiChokuretsuLib.Archive.Data;

namespace SerialLoops.Lib.Items
{
    public class PuzzleItem : Item
    {
        public PuzzleFile Puzzle { get; set; }

        public PuzzleItem(PuzzleFile puzzleFile) : base(puzzleFile.Name[0..^1], ItemType.Puzzle)
        {
            Puzzle = puzzleFile;
        }
    }
}
