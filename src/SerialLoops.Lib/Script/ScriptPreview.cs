using System.Collections.Generic;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;

namespace SerialLoops.Lib.Script;

public class ScriptPreview
{
    public short EpisodeHeader { get; set; }
    public BackgroundItem Kbg { get; set; }
    public BackgroundMusicItem Bgm { get; set; }
    public PlaceItem Place { get; set; }
    public List<PositionedChibi> TopScreenChibis { get; set; } = [];
    public (int InternalYOffset, int ExternalXOffset, ChibiItem EmotingChibi) ChibiEmote { get; set; }
    public SKColor? FadedColor { get; set; }
    public ScreenScriptParameter.DsScreen FadedScreens { get; set; }
    public ScriptItemCommand CurrentFade { get; set; }
    public BackgroundItem Background { get; set; }
    public BackgroundItem PrevFadeBackground { get; set; }
    public short BgFadeFrames { get; set; }
    public PaletteEffectScriptParameter.PaletteEffect PalEffect { get; set; }
    public ScriptItemCommand BgScrollCommand { get; set; }
    public bool BgPositionBool { get; set; }
    public (ItemItem Item, ItemItem.ItemLocation Location, ItemItem.ItemTransition Transition) Item { get; set; }
    public ItemItem.ItemLocation ItemPreviousLocation { get; set; }
    public List<PositionedSprite> Sprites { get; set; } = [];
    public ScriptItemCommand LastDialogueCommand { get; set; }
    public TopicItem Topic { get; set; }
    public List<string> CurrentChoices { get; set; }
    public bool HaruhiMeterVisible { get; set; }
    public bool ChessMode { get; set; }
    public ChessPuzzleItem ChessPuzzle { get; set; }
    public List<short> ChessHighlightedSpaces { get; set; } = [];
    public List<short> ChessGuidePieces { get; set; } = [];
    public List<short> ChessGuideSpaces { get; set; } = [];
    public List<short> ChessCrossedSpaces { get; set; } = [];
    public string ErrorImage { get; set; }
}
