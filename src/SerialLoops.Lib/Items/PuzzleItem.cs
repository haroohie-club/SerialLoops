using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;

namespace SerialLoops.Lib.Items
{
    public class PuzzleItem : Item
    {
        public PuzzleFile Puzzle { get; set; }

        public PuzzleItem(PuzzleFile puzzleFile) : base(puzzleFile.Name[0..^1], ItemType.Puzzle)
        {
            Puzzle = puzzleFile;
        }

        public override void Refresh(ArchiveFile<DataFile> dat, ArchiveFile<EventFile> evt, ArchiveFile<GraphicsFile> grp)
        {
            throw new System.NotImplementedException();
        }
    }
}
