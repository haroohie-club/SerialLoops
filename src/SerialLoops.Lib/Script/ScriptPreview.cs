using System.Collections.Generic;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.Lib.Script
{
    public class ScriptPreview
    {
        public short EpisodeHeader { get; set; }
        public BackgroundItem Kbg { get; set; }
        public PlaceItem Place { get; set; }
        public List<(ChibiItem Chibi, int X, int Y)> TopScreenChibis { get; set; } = [];
        public (int InternalYOffset, int ExternalXOffset, ChibiItem EmotingChibi) ChibiEmote { get; set; }
        public BackgroundItem Background { get; set; }
        public PaletteEffectScriptParameter.PaletteEffect BgPalEffect { get; set; }
        public ScriptItemCommand BgScrollCommand { get; set; }
        public bool BgPositionBool { get; set; }
        public (ItemItem Item, ItemItem.ItemLocation Location) Item { get; set; }
        public List<PositionedSprite> Sprites { get; set; } = [];
        public ScriptItemCommand LastDialogueCommand { get; set; }
        public string ErrorImage { get; set; }
    }
}
