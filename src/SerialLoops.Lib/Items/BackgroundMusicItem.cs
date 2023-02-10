using HaruhiChokuretsuLib.Archive.Data;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class BackgroundMusicItem : Item
    {
        public string BgmFile { get; set; }
        public int Index { get; set; }
        public string BgmName { get; set; }
        public string ExtrasShort { get; set; }

        public BackgroundMusicItem(string bgmFile, ExtraFile extras) : base(bgmFile, ItemType.BGM)
        {
            Name = Path.GetFileNameWithoutExtension(bgmFile);
            BgmFile = bgmFile;
            Index = int.Parse(Name[^3..]);
            BgmName = extras.Bgms.FirstOrDefault(b => b.Index == Index).Name ?? "";
        }

        public override void Refresh(Project project)
        {
        }
    }
}
