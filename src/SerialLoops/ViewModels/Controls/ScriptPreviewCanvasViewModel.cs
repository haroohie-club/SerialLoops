using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using AvaloniaEdit.Utils;
using HaruhiChokuretsuLib.Archive.Data;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Models;
using SkiaSharp;

namespace SerialLoops.ViewModels.Controls;

public class ScriptPreviewCanvasViewModel(Project project) : ReactiveObject
{
    public ScriptPreviewCanvas PreviewCanvas { get; set; }

    private ScriptPreview _preview;
    public ScriptPreview Preview
    {
        get => _preview;
        set
        {
            ScriptPreview previous = _preview;
            this.RaiseAndSetIfChanged(ref _preview, value);

            if (_preview is null)
            {
                return;
            }

            if (previous?.EpisodeHeader != _preview.EpisodeHeader)
            {
                if (_preview.EpisodeHeader == (short)EpisodeHeaderScriptParameter.Episode.None)
                {
                    EpisodeHeader = null;
                    EpisodeHeaderVisible = false;
                    KbgVisible = true;
                    PlaceVisible = true;
                    TopScreenChibisVisible = true;
                }
                else
                {
                    EpisodeHeader = new(EpisodeHeaderScriptParameter
                        .GetTexture((EpisodeHeaderScriptParameter.Episode)_preview.EpisodeHeader, project).GetTexture());
                    EpisodeHeaderVisible = true;
                    KbgVisible = false;
                    PlaceVisible = false;
                    TopScreenChibisVisible = false;
                }
            }

            if (previous?.Kbg != _preview.Kbg)
            {
                Kbg = new(_preview.Kbg?.GetBackground());
            }

            if (previous?.Place != _preview.Place)
            {
                Place = new(_preview.Place?.GetPreview(project));
            }

            TopScreenChibis.Clear();
            TopScreenChibis.AddRange(_preview.TopScreenChibis.Select(c => new AnimatedPositionedChibi(c)));

            if (_preview.ChibiEmote.EmotingChibi is not null)
            {
                SKBitmap emotes = project.Grp.GetFileByName("SYS_ADV_T08DNX").GetImage(width: 32, transparentIndex: 0);
                SKBitmap emote = new(32, 32);
                emotes.ExtractSubset(emote, new(0, _preview.ChibiEmote.InternalYOffset, 32, _preview.ChibiEmote.InternalYOffset + 32));
                int chibiY = _preview.TopScreenChibis.First(c => c.Chibi == _preview.ChibiEmote.EmotingChibi).Y;
                ChibiEmote = new(new(emote), _preview.ChibiEmote.ExternalXOffset + 16, chibiY - 32);
            }
            else
            {
                ChibiEmote = null;
            }

            if (previous?.Background != _preview.Background || previous?.BgPositionBool != _preview.BgPositionBool || previous?.BgScrollCommand != _preview.BgScrollCommand)
            {
                if (_preview.Background is null)
                {
                    TopScreenCg = null;
                    BottomScreenCg = null;
                    Bg = null;
                }
                else
                {
                    SKBitmap topScreenBitmap = new(256, 192);
                    SKBitmap bottomScreenBitmap = new(256, 192);

                    switch (_preview.Background.BackgroundType)
                    {
                        case BgType.TEX_CG_DUAL_SCREEN:
                            SKBitmap dualScreenBg = _preview.Background.GetBackground();
                            if (_preview.BgScrollCommand is not null &&
                                ((BgScrollDirectionScriptParameter)_preview.BgScrollCommand.Parameters[0]).ScrollDirection ==
                                BgScrollDirectionScriptParameter.BgScrollDirection.DOWN)
                            {

                                dualScreenBg.ExtractSubset(topScreenBitmap, new(0, _preview.Background.Graphic2.Height - 192, 256, _preview.Background.Graphic2.Height));
                                int bottomScreenY = dualScreenBg.Height - 192;
                                dualScreenBg.ExtractSubset(bottomScreenBitmap, new(0, bottomScreenY, 256, bottomScreenY + 192));
                            }
                            else
                            {
                                dualScreenBg.ExtractSubset(topScreenBitmap, new(0, 0, 256, 192));
                                dualScreenBg.ExtractSubset(bottomScreenBitmap, new(0, _preview.Background.Graphic2.Height, 256, _preview.Background.Graphic2.Height + 192));
                            }
                            TopScreenCg = new(topScreenBitmap);
                            BottomScreenCg = new(bottomScreenBitmap);
                            break;

                        case BgType.TEX_CG_SINGLE:
                            TopScreenCg = null;
                            if (_preview.BgPositionBool || (_preview.BgScrollCommand is not null &&
                                                           ((BgScrollDirectionScriptParameter)_preview.BgScrollCommand
                                                               .Parameters[0]).ScrollDirection ==
                                                           BgScrollDirectionScriptParameter.BgScrollDirection.DOWN))
                            {
                                SKBitmap bgBitmap = _preview.Background.GetBackground();
                                bgBitmap.ExtractSubset(bottomScreenBitmap,
                                    new(0, bgBitmap.Height - 192, bgBitmap.Width, bgBitmap.Height));
                            }
                            else
                            {
                                _preview.Background.GetBackground()
                                    .ExtractSubset(bottomScreenBitmap, new(0, 0, 256, 192));
                            }
                            BottomScreenCg = new(bottomScreenBitmap);
                            break;

                        case BgType.TEX_CG_WIDE:
                            TopScreenCg = null;
                            if (_preview.BgScrollCommand is not null &&
                                ((BgScrollDirectionScriptParameter)_preview.BgScrollCommand.Parameters[0]).ScrollDirection ==
                                BgScrollDirectionScriptParameter.BgScrollDirection.RIGHT)
                            {
                                SKBitmap bgBitmap = _preview.Background.GetBackground();
                                bgBitmap.ExtractSubset(bottomScreenBitmap,
                                    new(bgBitmap.Width - 256, 0, bgBitmap.Width, 192));
                            }
                            else
                            {
                                _preview.Background.GetBackground().ExtractSubset(bottomScreenBitmap, new(0, 0, 256, 192));
                            }
                            BottomScreenCg = new(bottomScreenBitmap);
                            break;

                        case BgType.TEX_CG:
                            TopScreenCg = null;
                            BottomScreenCg = new(_preview.Background.GetBackground());
                            break;

                        default:
                        {
                            TopScreenCg = null;
                            BottomScreenCg = null;
                            using SKCanvas canvas = new(bottomScreenBitmap);
                            canvas.DrawBitmap(_preview.Background.GetBackground(), new SKPoint(0, 0),
                                PaletteEffectScriptParameter.GetPaletteEffectPaint(_preview.PalEffect));
                            canvas.Flush();
                            Bg = new(bottomScreenBitmap);
                            BgOrigin = _preview.ChessMode ? new(0, 0) : new(0, 192);
                            break;
                        }
                    }
                }
            }

            if (previous?.Item != _preview.Item)
            {
                if (_preview.Item.Item is null)
                {
                    Item = null;
                }
                else
                {
                    Item = new(_preview.Item.Item, _preview.Item.Location, _preview.Item.Transition, _preview.ItemPreviousLocation, ChessMode ? 0 : 192);
                }
            }

            PreviewCanvas.RunNonLoopingAnimations();
        }
    }

    [Reactive]
    public SKAvaloniaImage EpisodeHeader { get; set; }
    [Reactive]
    public bool EpisodeHeaderVisible { get; set; }

    [Reactive]
    public SKAvaloniaImage Kbg { get; set; }
    [Reactive]
    public bool KbgVisible { get; set; }

    [Reactive]
    public SKAvaloniaImage Place { get; set; }
    [Reactive]
    public bool PlaceVisible { get; set; }

    public ObservableCollection<AnimatedPositionedChibi> TopScreenChibis { get; set; } = [];
    [Reactive]
    public bool TopScreenChibisVisible { get; set; }
    [Reactive]
    public PositionedChibiEmote ChibiEmote { get; set; }


    [Reactive]
    public SKAvaloniaImage TopScreenCg { get; set; }
    [Reactive]
    public SKAvaloniaImage BottomScreenCg { get; set; }
    [Reactive]
    public SKAvaloniaImage Bg { get; set; }
    [Reactive]
    public Point BgOrigin { get; set; }

    [Reactive]
    public AnimatedPositionedItem Item { get; set; }

    public ObservableCollection<PositionedSprite> Sprites { get; set; } = [];

    [Reactive]
    public ScriptItemCommand LastDialogueCommand { get; set; }

    [Reactive]
    public SKAvaloniaImage TopicChyron { get; set; }

    public ObservableCollection<string> CurrentChoices { get; set; }

    [Reactive]
    public SKAvaloniaImage HaruhiMeter { get; set; }

    [Reactive]
    public bool ChessMode { get; set; }
    [Reactive]
    public ChessPuzzleItem ChessPuzzle { get; set; }
    public ObservableCollection<short> ChessHighlightedSpaces { get; set; } = [];
    public ObservableCollection<short> ChessGuidePieces { get; set; } = [];
    public ObservableCollection<short> ChessGuideSpaces { get; set; } = [];
    public ObservableCollection<short> ChessCrossedSpaces { get; set; } = [];

    [Reactive]
    public SKAvaloniaImage ErrorImage { get; set; }
}

public class AnimatedPositionedChibi(PositionedChibi chibi) : ReactiveObject
{
    private PositionedChibi _chibi = chibi;
    public PositionedChibi Chibi
    {
        get => _chibi;
        set
        {
            this.RaiseAndSetIfChanged(ref _chibi, value);
            AnimatedImage = new(_chibi.Chibi.ChibiAnimations.ElementAt(0).Value);
        }
    }

    public AnimatedImageViewModel AnimatedImage { get; set; } = new(chibi.Chibi.ChibiAnimations.ElementAt(0).Value);
}

public class PositionedChibiEmote(SKAvaloniaImage emote, int x, int y)
{
    public SKAvaloniaImage Emote { get; set; } = emote;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double AnimatedY { get; } = y - 8;
}

public class AnimatedPositionedItem
{
    public SKAvaloniaImage Image { get; }
    public Point FinalPosition { get; }
    public Point StartPosition { get; }
    public double StartOpacity { get; }

    public AnimatedPositionedItem(ItemItem item, ItemItem.ItemLocation location, ItemItem.ItemTransition transition,
        ItemItem.ItemLocation previousItemLocation, int verticalOffset)
    {
        FinalPosition = GetItemPosition(location, item.ItemGraphic.Width, verticalOffset);
        if (FinalPosition.X < 0)
        {
            FinalPosition = GetItemPosition(previousItemLocation, item.ItemGraphic.Width, verticalOffset);
            if (FinalPosition.X < 0)
            {
                Image = null;
                return;
            }
        }
        Image = new(item.ItemGraphic.GetImage(transparentIndex: 0));
        if (transition == 0)
        {
            StartOpacity = 1;
            StartPosition = FinalPosition;
            return;
        }

        StartOpacity = 0;
        StartPosition = transition == ItemItem.ItemTransition.Fade
            ? FinalPosition
            : location switch
            {
                ItemItem.ItemLocation.Left => new(FinalPosition.X - 32, FinalPosition.Y),
                ItemItem.ItemLocation.Right => new(FinalPosition.X + 32, FinalPosition.Y),
                ItemItem.ItemLocation.Center => new(FinalPosition.X, FinalPosition.Y - 32),
                _ => new(0, 0),
            };
    }

    private static Point GetItemPosition(ItemItem.ItemLocation location, int width, int verticalOffset)
    {
        return location switch
        {
            ItemItem.ItemLocation.Left => new(128 - width, verticalOffset + 12),
            ItemItem.ItemLocation.Center => new(128 - width / 2, verticalOffset + 12),
            ItemItem.ItemLocation.Right => new(128, verticalOffset + 12),
            _ => new(-1, -1),
        };
    }
}
