namespace SerialLoops.Lib.Items
{
    public class BackgroundMusicItem : Item
    {
        public string BgmFile { get; set; }
        public int Index { get; set; }

        public BackgroundMusicItem(string bgmFile, int index) : base(bgmFile, ItemType.BGM)
        {
            BgmFile = bgmFile;
            Index = index;
        }

        public override void Refresh(Project project)
        {
        }
    }
}
