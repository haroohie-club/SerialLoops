using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Items
{
    public class BackgroundMusicItem : Item
    {
        public string BgmFile { get; set; }

        public BackgroundMusicItem(string bgmFile) : base(bgmFile, ItemType.BGM)
        {
            BgmFile = bgmFile;
        }

        public override void Refresh(Project project)
        {
        }
    }
}
