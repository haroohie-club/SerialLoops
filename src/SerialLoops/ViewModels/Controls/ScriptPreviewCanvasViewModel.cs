using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using AvaloniaEdit.Utils;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Utility;
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

            if (!string.IsNullOrEmpty(_preview.ErrorImage))
            {
                ErrorImage = new(AssetLoader.Open(new(_preview.ErrorImage)));
                DisplayError = true;
                return;
            }

            DisplayError = false;
            VerticalOffset = _preview.ChessMode ? 0 : 192;

            if (_preview.FadedColor is not null || _preview.CurrentFade is not null)
            {
                if (_preview.CurrentFade is not null)
                {
                    switch (_preview.CurrentFade.Verb)
                    {
                       case EventFile.CommandVerb.SCREEN_FADEOUT:
                           FadeColor =
                               ((ColorMonochromeScriptParameter)_preview.CurrentFade.Parameters[4]).ColorType switch
                               {
                                   ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamCustom =>
                                       new ImmutableSolidColorBrush(((ColorScriptParameter)_preview.CurrentFade.Parameters[2]).Color.ToAvalonia()),
                                   ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamWhite => Brushes.White,
                                   _ => Brushes.Black,
                               };
                           FadeTime = TimeSpan.FromSeconds(((ShortScriptParameter)_preview.CurrentFade.Parameters[0]).Value / 60.0);
                           FadeTopScreen = ((ScreenScriptParameter)_preview.CurrentFade.Parameters[3]).Screen is ScreenScriptParameter.DsScreen.TOP or ScreenScriptParameter.DsScreen.BOTH;
                           FadeBottomScreen = ((ScreenScriptParameter)_preview.CurrentFade.Parameters[3]).Screen is ScreenScriptParameter.DsScreen.BOTTOM or ScreenScriptParameter.DsScreen.BOTH;
                           StartFadeOpacity = 0.0;
                           EndFadeOpacity = 1.0;
                           ConstantFadeOpacity = 0.5;
                           break;

                       case EventFile.CommandVerb.SCREEN_FADEIN:
                           FadeColor =
                               ((ColorMonochromeScriptParameter)_preview.CurrentFade.Parameters[3]).ColorType switch
                               {
                                   ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamCustom => new ImmutableSolidColorBrush(((SKColor)_preview.FadedColor!).ToAvalonia()),
                                   ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamWhite => Brushes.White,
                                   _ => Brushes.Black,
                               };
                           FadeTime = TimeSpan.FromSeconds(((ShortScriptParameter)_preview.CurrentFade.Parameters[0]).Value / 60.0);
                           FadeTopScreen = ((ScreenScriptParameter)_preview.CurrentFade.Parameters[2]).Screen is ScreenScriptParameter.DsScreen.TOP or ScreenScriptParameter.DsScreen.BOTH;
                           FadeBottomScreen = ((ScreenScriptParameter)_preview.CurrentFade.Parameters[2]).Screen is ScreenScriptParameter.DsScreen.BOTTOM or ScreenScriptParameter.DsScreen.BOTH;
                           StartFadeOpacity = 1.0;
                           EndFadeOpacity = 0.0;
                           ConstantFadeOpacity = 0.0;
                           break;

                       case EventFile.CommandVerb.INVEST_START:
                           FadeColor = Brushes.Black;
                           FadeTime = TimeSpan.FromSeconds(1.0);
                           FadeTopScreen = false;
                           FadeBottomScreen = true;
                           StartFadeOpacity = 0.0;
                           EndFadeOpacity = 1.0;
                           ConstantFadeOpacity = 0.0;
                           break;

                       case EventFile.CommandVerb.SCREEN_FLASH:
                           FadeColor = new ImmutableSolidColorBrush(((ColorScriptParameter)_preview.CurrentFade.Parameters[3]).Color.ToAvalonia());
                           short fadeInFrames = ((ShortScriptParameter)_preview.CurrentFade.Parameters[0]).Value;
                           short fadeOutFrames = ((ShortScriptParameter)_preview.CurrentFade.Parameters[2]).Value;
                           FlashTime = TimeSpan.FromSeconds(
                               (fadeInFrames + ((ShortScriptParameter)_preview.CurrentFade.Parameters[1]).Value + fadeOutFrames) / 60.0);
                           Hold1Percentage = fadeInFrames / 60.0 / FlashTime.TotalSeconds;
                           Hold2Percentage = 1 - fadeOutFrames / 60.0 / FlashTime.TotalSeconds;
                           break;
                    }
                }
                else
                {
                    FadeTime = TimeSpan.Zero;
                    FlashTime = TimeSpan.Zero;
                    FadeColor = new ImmutableSolidColorBrush(((SKColor)_preview.FadedColor!).ToAvalonia());
                    FadeTopScreen = _preview.FadedScreens is ScreenScriptParameter.DsScreen.TOP or ScreenScriptParameter.DsScreen.BOTH;
                    FadeBottomScreen = _preview.FadedScreens is ScreenScriptParameter.DsScreen.BOTTOM or ScreenScriptParameter.DsScreen.BOTH;
                    ConstantFadeOpacity = 0.5;
                }
            }
            else
            {
                FadeTime = TimeSpan.Zero;
                FlashTime = TimeSpan.Zero;
                FadeTopScreen = false;
                FadeBottomScreen = false;
                ConstantFadeOpacity = 0.0;
            }

            if (!_preview.ChessMode)
            {
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

                ChessBoard = null;
                ChessGuideSpaces.Clear();
                ChessHighlightedSpaces.Clear();
                ChessCrossedSpaces.Clear();
            }
            else if (_preview.ChessPuzzle is not null)
            {
                KbgVisible = false;
                PlaceVisible = false;
                TopScreenChibis.Clear();
                ChibiEmote = null;

                ChessBoard = new(_preview.ChessPuzzle.GetChessboard(project));

                ChessGuideSpaces.Clear();
                foreach (SKPoint rectOrigin in _preview.ChessGuideSpaces.Select(g => ChessPuzzleItem.GetChessPiecePosition(g)))
                {
                    ChessGuideSpaces.Add(new(rectOrigin.X + 5, rectOrigin.Y + 203));
                }

                ChessHighlightedSpaces.Clear();
                foreach (SKPoint rectOrigin in _preview.ChessHighlightedSpaces.Select(h => ChessPuzzleItem.GetChessSpacePosition(h)))
                {
                    ChessHighlightedSpaces.Add(new(rectOrigin.X + 5, rectOrigin.Y + 203));
                }

                ChessCrossedSpaces.Clear();
                foreach (SKPoint rectOrigin in _preview.ChessCrossedSpaces.Select(c => ChessPuzzleItem.GetChessSpacePosition(c)))
                {
                    ChessCrossedSpaces.Add(new(rectOrigin.X + 5, rectOrigin.Y + 203));
                }
            }

            if (previous?.Background != _preview.Background || previous?.BgPositionBool != _preview.BgPositionBool ||
                previous?.BgScrollCommand != _preview.BgScrollCommand || previous?.PalEffect != _preview.PalEffect)
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
                                BgScrollDirectionScriptParameter.BgScrollDirection.BgScrollDown)
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
                                                           BgScrollDirectionScriptParameter.BgScrollDirection.BgScrollDown))
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
                                BgScrollDirectionScriptParameter.BgScrollDirection.BgScrollRight)
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
                            BgOrigin = new(0, VerticalOffset);
                            break;
                        }
                    }
                }
            }

            if (_preview.PrevFadeBackground is not null)
            {
                PreviousBg = new(_preview.PrevFadeBackground.GetBackground());
                BgFadeTime = TimeSpan.FromSeconds(_preview.BgFadeFrames / 60.0);
            }
            else
            {
                PreviousBg = null;
                BgFadeTime = TimeSpan.Zero;
            }

            if (previous?.Item != _preview.Item)
            {
                if (_preview.Item.Item is null)
                {
                    Item = null;
                }
                else
                {
                    Item = new(_preview.Item.Item, _preview.Item.Location, _preview.Item.Transition, _preview.ItemPreviousLocation, VerticalOffset);
                }
            }

            Sprites.Clear();
            Sprites.AddRange(_preview.Sprites.Select(s => new AnimatedPositionedSprite(s, project)));

            if (_preview.LastDialogueCommand is not null)
            {
                DialogueLine line = ((DialogueScriptParameter)_preview.LastDialogueCommand.Parameters[0]).Line;
                SKPaint dialoguePaint = _preview.LastDialogueCommand.Verb == EventFile.CommandVerb.PIN_MNL
                    ? project.DialogueColorFilters[1]
                    : line.Speaker switch
                    {
                        Speaker.MONOLOGUE => project.DialogueColorFilters[1],
                        Speaker.INFO => project.DialogueColorFilters[4],
                        _ => project.DialogueColorFilters[0],
                    };
                if (!string.IsNullOrEmpty(line.Text))
                {
                    SKBitmap dialogueBitmap = new(256, 52);
                    using SKCanvas canvas = new(dialogueBitmap);

                    canvas.DrawBitmap(project.DialogueBitmap, new(0, 24, 32, 36), new SKRect(0, 12, 256, 24));
                    SKColor dialogueBoxColor = project.DialogueBitmap.GetPixel(0, 28);
                    canvas.DrawRect(0, 24, 256, 28, new() { Color = dialogueBoxColor });
                    canvas.DrawBitmap(project.DialogueBitmap, new(0, 37, 32, 64),
                        new SKRect(224, 25, 256, 52));
                    if (_preview.LastDialogueCommand.Verb != EventFile.CommandVerb.PIN_MNL)
                    {
                        canvas.DrawBitmap(project.SpeakerBitmap,
                            new(0, 16 * ((int)line.Speaker - 1), 64, 16 * ((int)line.Speaker)),
                            new SKRect(0, 0, 64, 16));
                    }

                    canvas.DrawHaroohieText(line.Text, dialoguePaint, project, y: 20);
                    canvas.DrawBitmap(project.DialogueArrow, new(0, 0, 16, 16), new SKRect(240, 36, 256, 52));
                    canvas.Flush();
                    Dialogue = new(dialogueBitmap);
                }

                DialogueY = VerticalOffset + 140;
            }
            else
            {
                Dialogue = null;
            }

            if (_preview.Topic is not null)
            {
                SKBitmap flyoutSysTex = project.Grp.GetFileByName("SYS_ADV_B01DNX").GetImage(transparentIndex: 0);
                SKBitmap topicFlyout = new(76, 32);
                using SKCanvas flyoutCanvas = new(topicFlyout);

                flyoutCanvas.DrawBitmap(flyoutSysTex, new(0, 20, 32, 32),
                    new SKRect(0, 12, 32, 24));
                flyoutCanvas.DrawBitmap(flyoutSysTex, new(0, 0, 44, 20),
                    new SKRect(32, 6, 76, 26));

                SKBitmap topicCards = project.Grp.GetFileByName("SYS_CMN_B09DNX").GetImage(transparentIndex: 0);
                int srcX = _preview.Topic.TopicEntry.CardType switch
                {
                    TopicCardType.Haruhi => 0,
                    TopicCardType.Mikuru => 20,
                    TopicCardType.Nagato => 40,
                    TopicCardType.Koizumi => 60,
                    TopicCardType.Main => 80,
                    _ => 100,
                };

                flyoutCanvas.DrawBitmap(topicCards, new(srcX, 0, srcX + 20, 24),
                    new SKRect(10, 2, 30, 26));
                flyoutCanvas.Flush();
                TopicFlyout = new(topicFlyout);
                TopicFlyoutY = VerticalOffset + 128;
            }
            else
            {
                TopicFlyout = null;
            }

            CurrentChoices.Clear();
            if (_preview.CurrentChoices?.Count > 0)
            {
                List<SKBitmap> choiceGraphics = [];
                foreach (string choice in _preview.CurrentChoices)
                {
                    SKBitmap choiceGraphic = new(218, 18);
                    using SKCanvas choiceCanvas = new(choiceGraphic);
                    choiceCanvas.DrawRect(1, 1, 216, 16, new() { Color = new(146, 146, 146) });
                    choiceCanvas.DrawRect(2, 2, 214, 14, new() { Color = new(69, 69, 69) });
                    int choiceWidth = choice.CalculateHaroohieTextWidth(project);
                    choiceCanvas.DrawHaroohieText(choice, project.DialogueColorFilters[0], project, (218 - choiceWidth) / 2, 2);
                    choiceCanvas.Flush();
                    choiceGraphics.Add(choiceGraphic);
                }

                int graphicY = (192 - (choiceGraphics.Count * 18 + (choiceGraphics.Count - 1) * 8)) / 2 + 184;
                foreach (SKBitmap choiceGraphic in choiceGraphics)
                {
                    CurrentChoices.Add(new(new(choiceGraphic), graphicY));
                    graphicY += 26;
                }
            }

            HaruhiMeterVisible = _preview.HaruhiMeterVisible;

            PreviewCanvas.RunNonLoopingAnimations();
        }
    }

    [Reactive]
    public bool FadeTopScreen { get; set; }
    [Reactive]
    public bool FadeBottomScreen { get; set; }
    [Reactive]
    public IImmutableSolidColorBrush FadeColor { get; set; }
    [Reactive]
    public TimeSpan FadeTime { get; set; }
    [Reactive]
    public double StartFadeOpacity { get; set; }
    [Reactive]
    public double EndFadeOpacity { get; set; }
    [Reactive]
    public double ConstantFadeOpacity { get; set; }

    [Reactive]
    public TimeSpan FlashTime { get; set; }
    [Reactive]
    public double Hold1Percentage { get; set; }
    [Reactive]
    public double Hold2Percentage { get; set; }

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
    public SKAvaloniaImage PreviousBg { get; set; }
    [Reactive]
    public TimeSpan BgFadeTime { get; set; }

    [Reactive]
    public AnimatedPositionedItem Item { get; set; }

    public ObservableCollection<AnimatedPositionedSprite> Sprites { get; set; } = [];
    [Reactive]
    public int VerticalOffset { get; set; }

    [Reactive]
    public SKAvaloniaImage Dialogue { get; set; }
    [Reactive]
    public int DialogueY { get; set; }

    [Reactive]
    public SKAvaloniaImage TopicFlyout { get; set; }
    [Reactive]
    public int TopicFlyoutY { get; set; }

    public ObservableCollection<PositionedChoice> CurrentChoices { get; set; } = [];

    public SKAvaloniaImage HaruhiMeter { get; } = new(((SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_B14")).GetTexture());
    [Reactive]
    public bool HaruhiMeterVisible { get; set; }

    [Reactive]
    public bool ChessMode { get; set; }
    [Reactive]
    public SKAvaloniaImage ChessBoard { get; set; }
    public ObservableCollection<Point> ChessHighlightedSpaces { get; set; } = [];
    public ObservableCollection<Point> ChessGuideSpaces { get; set; } = [];
    public ObservableCollection<Point> ChessCrossedSpaces { get; set; } = [];

    [Reactive]
    public Bitmap ErrorImage { get; set; }
    [Reactive]
    public bool DisplayError { get; set; }
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

    [Reactive]
    public AnimatedImageViewModel AnimatedImage { get; set; } = new(chibi.Chibi.ChibiAnimations.ElementAt(0).Value);
}

public class AnimatedPositionedSprite : ReactiveObject
{
    private Project _project;
    private PositionedSprite _sprite;
    public PositionedSprite Sprite
    {
        get => _sprite;
        set
        {
            this.RaiseAndSetIfChanged(ref _sprite, value);
            AnimatedImage = new(_sprite.Sprite.GetClosedMouthAnimation(_project));
        }
    }

    [Reactive]
    public int YPosition { get; set; }

    [Reactive]
    public double StartXPosition { get; set; }
    [Reactive]
    public double EndXPosition { get; set; }
    [Reactive]
    public Easing AnimEasing { get; set; }
    [Reactive]
    public double StartOpacity { get; set; }
    [Reactive]
    public double EndOpacity { get; set; }
    [Reactive]
    public TimeSpan AnimDuration { get; set; }

    [Reactive]
    public AnimatedImageViewModel AnimatedImage { get; set; }

    public AnimatedPositionedSprite(PositionedSprite sprite, Project project)
    {
        _project = project;
        _sprite = sprite;
        var framesWithTimings = sprite.Sprite.GetClosedMouthAnimation(project);
        if (sprite.PalEffect is not null)
        {
            for (int i = 0; i < framesWithTimings.Count; i++)
            {
                SKBitmap tintedFrame = new(framesWithTimings[i].Frame.Width, framesWithTimings[i].Frame.Height);
                using SKCanvas canvas = new(tintedFrame);
                canvas.DrawBitmap(framesWithTimings[i].Frame, 0, 0, sprite.PalEffect);
                canvas.Flush();
                framesWithTimings[i] = (tintedFrame, framesWithTimings[i].Timing);
            }
        }
        AnimatedImage = new(framesWithTimings);
        YPosition  = 192 - AnimatedImage.CurrentFrame.Height;

        if (sprite.PreTransition != SpritePreTransitionScriptParameter.SpritePreTransition.NO_TRANSITION)
        {
            EndXPosition = sprite.Positioning.X;
            EndOpacity = 1.0;
            switch (sprite.PreTransition)
            {
                case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_LEFT_TO_RIGHT:
                case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_LEFT_TO_CENTER:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = 256;
                    StartOpacity = 0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_RIGHT_TO_LEFT:
                case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_RIGHT_TO_CENTER:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = -framesWithTimings[0].Frame.Width;
                    StartOpacity = 0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePreTransitionScriptParameter.SpritePreTransition.BOUNCE_LEFT_TO_RIGHT:
                    AnimDuration = TimeSpan.FromSeconds(1);
                    StartXPosition = EndXPosition + 32;
                    StartOpacity = 1.0;
                    AnimEasing = new ElasticEaseOut();
                    break;

                case SpritePreTransitionScriptParameter.SpritePreTransition.BOUNCE_RIGHT_TO_LEFT:
                    AnimDuration = TimeSpan.FromSeconds(1);
                    StartXPosition = EndXPosition - 32;
                    StartOpacity = 1.0;
                    AnimEasing = new ElasticEaseOut();
                    break;

                case SpritePreTransitionScriptParameter.SpritePreTransition.FADE_TO_CENTER:
                case SpritePreTransitionScriptParameter.SpritePreTransition.FADE_IN_LEFT:
                    AnimDuration = TimeSpan.FromSeconds(0.5);
                    StartXPosition = EndXPosition;
                    StartOpacity = 0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_LEFT:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = sprite.StartPosition == 0 ? 256 :  sprite.StartPosition;
                    StartOpacity = 1;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_RIGHT:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = sprite.StartPosition == 0 ? -framesWithTimings[0].Frame.Width :  sprite.StartPosition;
                    StartOpacity = 1;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePreTransitionScriptParameter.SpritePreTransition.PEEK_RIGHT_TO_LEFT:
                    AnimDuration = TimeSpan.FromSeconds(1.5);
                    StartXPosition = -framesWithTimings[0].Frame.Width;
                    StartOpacity = 1;
                    AnimEasing = new BounceEaseInOut();
                    break;
            }
        }
        else if (sprite.PostTransition != SpritePostTransitionScriptParameter.SpritePostTransition.NO_EXIT)
        {
            switch (sprite.PostTransition)
            {
                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = SpritePositioning.SpritePosition.CENTER.GetSpriteX();
                    EndXPosition = 256;
                    StartOpacity = 1.0;
                    EndOpacity = 0.0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = SpritePositioning.SpritePosition.CENTER.GetSpriteX();
                    EndXPosition = -framesWithTimings[0].Frame.Width;
                    StartOpacity = 1.0;
                    EndOpacity = 0.0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_CENTER_TO_RIGHT_AND_STAY:
                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_CENTER_TO_LEFT_AND_STAY:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = SpritePositioning.SpritePosition.CENTER.GetSpriteX();
                    EndXPosition = sprite.Positioning.X;
                    StartOpacity = 1.0;
                    EndOpacity = 1.0;
                    AnimEasing = new QuadraticEaseInOut();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_RIGHT_FADE_OUT:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = SpritePositioning.SpritePosition.LEFT.GetSpriteX();
                    EndXPosition = 128;
                    StartOpacity = 1.0;
                    EndOpacity = 0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_LEFT_FADE_OUT:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = SpritePositioning.SpritePosition.LEFT.GetSpriteX();
                    EndXPosition = -framesWithTimings[0].Frame.Width;
                    StartOpacity = 1.0;
                    EndOpacity = 0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.FADE_OUT_CENTER:
                    AnimDuration = TimeSpan.FromSeconds(0.5);
                    StartXPosition = SpritePositioning.SpritePosition.CENTER.GetSpriteX();
                    EndXPosition = SpritePositioning.SpritePosition.CENTER.GetSpriteX();
                    StartOpacity = 1.0;
                    EndOpacity = 0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.FADE_OUT_LEFT:
                    AnimDuration = TimeSpan.FromSeconds(0.5);
                    StartXPosition = SpritePositioning.SpritePosition.LEFT.GetSpriteX();
                    EndXPosition = SpritePositioning.SpritePosition.LEFT.GetSpriteX();
                    StartOpacity = 1.0;
                    EndOpacity = 0;
                    AnimEasing = new LinearEasing();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_LEFT_TO_RIGHT_AND_STAY:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = SpritePositioning.SpritePosition.LEFT.GetSpriteX();
                    EndXPosition = sprite.Positioning.X;
                    StartOpacity = 1.0;
                    EndOpacity = 1.0;
                    AnimEasing = new QuadraticEaseInOut();
                    break;

                case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_RIGHT_TO_LEFT_AND_STAY:
                    AnimDuration = TimeSpan.FromSeconds(0.25);
                    StartXPosition = SpritePositioning.SpritePosition.RIGHT.GetSpriteX();
                    EndXPosition = sprite.Positioning.X;
                    StartOpacity = 1.0;
                    EndOpacity = 1.0;
                    AnimEasing = new QuadraticEaseInOut();
                    break;
            }
        }
        else
        {
            AnimDuration = TimeSpan.Zero;
            StartXPosition = EndXPosition;
            EndXPosition = sprite.Positioning.X;
            StartOpacity = 1.0;
            EndOpacity = 1.0;
            AnimEasing = new LinearEasing();
        }
    }
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
    public double FinalOpacity { get; } = 1.0;

    public AnimatedPositionedItem(ItemItem item, ItemItem.ItemLocation location, ItemItem.ItemTransition transition,
        ItemItem.ItemLocation previousItemLocation, int verticalOffset)
    {
        FinalPosition = GetItemPosition(location, item.ItemGraphic.Width, verticalOffset);
        if (FinalPosition.X < 0)
        {
            FinalPosition = GetItemPosition(previousItemLocation, item.ItemGraphic.Width, verticalOffset);
            if (FinalPosition.X < 0 || transition == 0)
            {
                Image = null;
            }
            else
            {
                Image = new(item.ItemGraphic.GetImage(transparentIndex: 0));
                StartOpacity = 1;
                FinalOpacity = transition == ItemItem.ItemTransition.Fade ? 0 : 1;
                StartPosition = FinalPosition;
                FinalPosition = transition == ItemItem.ItemTransition.Fade
                    ? FinalPosition
                    : location switch
                    {
                        ItemItem.ItemLocation.Left => new(-item.ItemGraphic.Width, FinalPosition.Y),
                        ItemItem.ItemLocation.Right => new(256, FinalPosition.Y),
                        ItemItem.ItemLocation.Center => new(FinalPosition.X, verticalOffset - item.ItemGraphic.Height),
                        _ => new(0, 0),
                    };
            }
            return;
        }
        Image = new(item.ItemGraphic.GetImage(transparentIndex: 0));
        if (transition == 0)
        {
            StartOpacity = 1;
            StartPosition = FinalPosition;
            return;
        }

        StartOpacity = transition == ItemItem.ItemTransition.Fade ? 0 : 1;
        StartPosition = transition == ItemItem.ItemTransition.Fade
            ? FinalPosition
            : location switch
            {
                ItemItem.ItemLocation.Left => new(-item.ItemGraphic.Width, FinalPosition.Y),
                ItemItem.ItemLocation.Right => new(256 + item.ItemGraphic.Width, FinalPosition.Y),
                ItemItem.ItemLocation.Center => new(FinalPosition.X, verticalOffset - item.ItemGraphic.Height),
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

public class PositionedChoice(SKAvaloniaImage choice, int y)
{
    public SKAvaloniaImage Graphic { get; } = choice;
    public int Y { get; } = y;
}
