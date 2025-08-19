using System;
using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using QuikGraph;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SkiaSharp;
using SoftCircuits.Collections;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Lib.Items;

public class ScriptItem : Item
{
    public EventFile Event { get; set; }
    public short StartReadFlag { get; set; }
    public short SfxGroupIndex { get; set; }
    public AdjacencyGraph<ScriptSection, ScriptSectionEdge> Graph { get; set; } = new();
    private readonly Func<string, string> _localize;

    public ScriptItem(string name) : base(name, ItemType.Script)
    {
    }

    public ScriptItem(EventFile evt, EventTable evtTbl, Func<string, string> localize, ILogger log) : base(
        evt.Name[..^1], ItemType.Script)
    {
        Event = evt;
        _localize = localize;

        PruneLabelsSection(log);
        Graph.AddVertexRange(Event.ScriptSections);
        UpdateEventTableInfo(evtTbl);
    }

    public OrderedDictionary<ScriptSection, List<ScriptItemCommand>> GetScriptCommandTree(Project project, ILogger log)
    {
        ScriptCommandInvocation currentCommand = null;
        try
        {
            OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commands = [];
            foreach (ScriptSection section in Event.ScriptSections)
            {
                commands.Add(section, []);
                foreach (ScriptCommandInvocation command in section.Objects)
                {
                    currentCommand = command;
                    commands[section].Add(ScriptItemCommand.FromInvocation(command, section,
                        commands[section].Count, Event, project, _localize, log));
                }
            }

            return commands;
        }
        catch (Exception ex)
        {
            log.LogException(
                string.Format(project.Localize("ErrorScriptCommandTree"),
                    DisplayName, Name, currentCommand?.Command.Mnemonic ?? "NULL_COMMAND", string.Join(", ", currentCommand?.Parameters ?? [])), ex);
            return null;
        }
    }

    public void CalculateGraphEdges(OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commandTree, ILogger log)
    {
        try
        {
            foreach (ScriptSection section in commandTree.Keys)
            {
                bool @continue = false;
                foreach (ScriptItemCommand command in commandTree[section])
                {
                    if (command.Verb == CommandVerb.INVEST_START)
                    {
                        Graph.AddEdge(new()
                        {
                            Source = section,
                            SourceCommandIndex = command.Index,
                            Target = ((ScriptSectionScriptParameter)command.Parameters[4]).Section,
                        });
                        Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                            Event.LabelsSection.Objects.Where(l =>
                                    Event.MapCharactersSection?.Objects.Select(c => c.TalkScriptBlock)
                                        .Contains(l.Id) ?? false)
                                .Select(l => l.Name.Replace("/", "")).Contains(s.Name)).Select(s =>
                            new ScriptSectionEdge { Source = section, Target = s }));
                        Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                            Event.LabelsSection.Objects.Where(l =>
                                    Event.InteractableObjectsSection?.Objects.Select(o => o.ScriptBlock)
                                        .Contains(l.Id) ?? false)
                                .Select(l => l.Name.Replace("/", "")).Contains(s.Name)).Select(s =>
                            new ScriptSectionEdge { Source = section, Target = s }));
                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.GOTO)
                    {
                        try
                        {
                            Graph.AddEdge(new()
                            {
                                Source = section,
                                SourceCommandIndex = command.Index,
                                Target = ((ScriptSectionScriptParameter)command.Parameters[0]).Section,
                            });
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            log.LogWarning(
                                "Failed to add graph edge for GOTO command as script section parameter was out of range.");
                        }

                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.VGOTO)
                    {
                        try
                        {
                            Graph.AddEdge(new()
                            {
                                Source = section,
                                SourceCommandIndex = command.Index,
                                Target = ((ScriptSectionScriptParameter)command.Parameters[1]).Section
                            });
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            log.LogWarning(
                                "Failed to add graph edge for VGOTO command as script section parameter was out of range.");
                        }
                    }
                    else if (command.Verb == CommandVerb.CHESS_VGOTO)
                    {
                        Graph.AddEdgeRange(command.Parameters.Cast<ScriptSectionScriptParameter>()
                            .Where(p => p.Section is not null).Select(p =>
                                new ScriptSectionEdge { Source = section, Target = p.Section }));
                        ScriptSection miss2Section =
                            Event.ScriptSections.FirstOrDefault(s => s.Name == "NONEMiss2");
                        if (miss2Section is not null)
                        {
                            Graph.AddEdge(new()
                            {
                                Source = section,
                                SourceCommandIndex = command.Index,
                                Target = Event.ScriptSections.First(s => s.Name == "NONEMiss2")
                            }); // hardcode this section, even tho you can't get to it
                        }
                    }
                    else if (command.Verb == CommandVerb.SELECT)
                    {
                        Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                                Event.LabelsSection.Objects.Where(l =>
                                        command.Parameters
                                            .Where(p => p.Type == ScriptParameter.ParameterType.OPTION)
                                            .Cast<OptionScriptParameter>()
                                            .Where(p => p.Option.Id > 0).Select(p => p.Option.Id).Contains(l.Id))
                                    .Select(l => l.Name.Replace("/", "")).Contains(s.Name))
                            .Select(s => new ScriptSectionEdge { Source = section, SourceCommandIndex = command.Index, Target = s }));
                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.NEXT_SCENE)
                    {
                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.BACK && section.Name != "SCRIPT00")
                    {
                        @continue = true;
                    }
                    else if (Name.StartsWith("CHS") && Name.EndsWith("90") &&
                             commandTree.Keys.ToList().IndexOf(section) > 1 && command.Index == 0)
                    {
                        Graph.AddEdge(new()
                        {
                            Source = Event.ScriptSections[1],
                            SourceCommandIndex = section.Objects.Count - 1,
                            Target = section,
                        }); // these particular chess files have no VGOTOs, so uh... we manually hardcode them
                    }
                }

                if (@continue)
                {
                    continue;
                }

                if (section != commandTree.Keys.Last())
                {
                    Graph.AddEdge(new()
                    {
                        Source = section,
                        SourceCommandIndex = section.Objects.Count - 1,
                        Target = commandTree.Keys.ElementAt(commandTree.Keys.ToList().IndexOf(section) + 1)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            log.LogException(_localize("ErrorFailedCalculatingScriptGraph"), ex);
            log.Log($"Script: {Name}, DisplayName: {DisplayName}");
        }
    }

    public ScriptPreview GetScriptPreview(OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commandTree,
        ScriptItemCommand currentCommand, Project project, ILogger log)
    {
        ScriptPreview preview = new();

        if (currentCommand is null)
        {
            return preview;
        }

        List<ScriptItemCommand> commands = currentCommand.WalkCommandGraph(commandTree, Graph);

        if (commands is null)
        {
            log.LogWarning($"No path found to current command.");
            preview.ErrorImage = "avares://SerialLoops/Assets/Graphics/ScriptPreviewError.png";
            return preview;
        }

        if (commandTree.SelectMany(kv => kv.Value).Any(c => c.Verb == CommandVerb.CHESS_LOAD))
        {
            preview.ChessMode = true;
        }

        // Get the BGM first
        ScriptItemCommand bgmCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.BGM_PLAY);
        if (bgmCommand is not null && ((BgmModeScriptParameter)bgmCommand.Parameters[1]).Mode == BgmModeScriptParameter.BgmMode.Start)
        {
            preview.Bgm = ((BgmScriptParameter)bgmCommand.Parameters[0]).Bgm;
        }

        // Render fades
        if (currentCommand.Verb is CommandVerb.SCREEN_FADEOUT or CommandVerb.SCREEN_FADEIN or CommandVerb.SCREEN_FLASH or CommandVerb.INVEST_START)
        {
            preview.CurrentFade = currentCommand;
            if (currentCommand.Verb == CommandVerb.SCREEN_FADEIN)
            {
                ScriptItemCommand prevFade = commands.FindLast(c => c.Verb is CommandVerb.SCREEN_FADEOUT or CommandVerb.INVEST_START);
                if (prevFade?.Verb == CommandVerb.SCREEN_FADEOUT)
                {
                    preview.FadedColor = ((ColorMonochromeScriptParameter)prevFade.Parameters[4]).ColorType switch
                    {
                        ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamCustom => ((ColorScriptParameter)prevFade.Parameters[2]).Color,
                        ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamBlack => SKColors.Black,
                        _ => SKColors.White,
                    };
                }
                else
                {
                    preview.FadedColor = SKColors.Black;
                }
            }
        }
        else if (commands.FindLastIndex(c =>
                     c.Verb is CommandVerb.SCREEN_FADEOUT or CommandVerb.INVEST_START) >
                 commands.FindLastIndex(c => c.Verb == CommandVerb.SCREEN_FADEIN))
        {
            if (commands.FindLastIndex(c => c.Verb == CommandVerb.SCREEN_FADEOUT) >
                commands.FindLastIndex(c => c.Verb is CommandVerb.INVEST_START))
            {
                ScriptItemCommand lastFadeOut = commands.FindLast(c => c.Verb == CommandVerb.SCREEN_FADEOUT);
                preview.FadedColor = ((ColorMonochromeScriptParameter)lastFadeOut.Parameters[4]).ColorType switch
                {
                    ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamCustom => ((ColorScriptParameter)lastFadeOut.Parameters[2]).Color,
                    ColorMonochromeScriptParameter.ColorMonochrome.ColorMonoParamBlack => SKColors.Black,
                    _ => SKColors.White,
                };
                preview.FadedScreens = ((ScreenScriptParameter)lastFadeOut.Parameters[3]).Screen;
            }
            else
            {
                preview.FadedColor = SKColors.Black;
                preview.FadedScreens = ScreenScriptParameter.DsScreen.BOTTOM;
            }
        }
        else if (commands.FindIndex(c => c.Verb == CommandVerb.SCREEN_FADEIN) < 0)
        {
            preview.FadedColor = SKColors.Black;
            preview.FadedScreens = ScreenScriptParameter.DsScreen.BOTH;
        }

        // If we're in chess mode, we don't need to draw any of the top screen stuff as the screens are flipped
        if (!preview.ChessMode)
        {
            if (commands.Any(c => c.Verb == CommandVerb.EPHEADER)
                && ((EpisodeHeaderScriptParameter)commands.Last(c => c.Verb == CommandVerb.EPHEADER).Parameters[0])
                .EpisodeHeaderIndex != EpisodeHeaderScriptParameter.Episode.None)
            {
                preview.EpisodeHeader =
                    (short)((EpisodeHeaderScriptParameter)commands.Last(c => c.Verb == CommandVerb.EPHEADER)
                        .Parameters[0]).EpisodeHeaderIndex;
            }

            // Draw top screen "kinetic" background
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Verb == CommandVerb.KBG_DISP &&
                    ((BgScriptParameter)commands[i].Parameters[0]).Background is not null)
                {
                    preview.Kbg = ((BgScriptParameter)commands[i].Parameters[0]).Background;
                    break;
                }
                if (commands[i].Verb == CommandVerb.OP_MODE)
                {
                    preview.Kbg = (BackgroundItem)project.Items.First(k => k.Name == "BG_KBG04");
                }
            }

            // Draw Place
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Verb == CommandVerb.SET_PLACE)
                {
                    if (((BoolScriptParameter)commands[i].Parameters[0]).Value &&
                        (((PlaceScriptParameter)commands[i].Parameters[1]).Place is not null))
                    {
                        preview.Place = ((PlaceScriptParameter)commands[i].Parameters[1]).Place;
                    }

                    break;
                }
            }

            // Draw top screen chibis
            List<ChibiItem> chibis = [];

            foreach (StartingChibiEntry chibi in Event.StartingChibisSection?.Objects ?? [])
            {
                if (chibi.ChibiIndex > 0)
                {
                    chibis.Add((ChibiItem)project.Items.First(i =>
                        i.Type == ItemType.Chibi && ((ChibiItem)i).TopScreenIndex == chibi.ChibiIndex));
                }
            }

            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].Verb == CommandVerb.OP_MODE)
                {
                    // Kyon auto-added by OP_MODE command
                    ChibiItem chibi = (ChibiItem)project.Items.First(i =>
                        i.Type == ItemType.Chibi && ((ChibiItem)i).TopScreenIndex == 1);
                    if (!chibis.Contains(chibi))
                    {
                        chibis.Add(chibi);
                    }
                }

                if (commands[i].Verb == CommandVerb.CHIBI_ENTEREXIT)
                {
                    if (((ChibiEnterExitScriptParameter)commands[i].Parameters[1]).Mode ==
                        ChibiEnterExitScriptParameter.ChibiEnterExitType.Enter)
                    {
                        ChibiItem chibi = ((ChibiScriptParameter)commands[i].Parameters[0]).Chibi;
                        if (!chibis.Contains(chibi))
                        {
                            if (chibi.TopScreenIndex < 1 || chibis.Count == 0)
                            {
                                chibis.Add(chibi);
                            }
                            else
                            {
                                bool inserted = false;
                                for (int j = 0; j < chibis.Count; j++)
                                {
                                    if (chibis[j].TopScreenIndex > chibi.TopScreenIndex)
                                    {
                                        chibis.Insert(j, chibi);
                                        inserted = true;
                                        break;
                                    }
                                }

                                if (!inserted)
                                {
                                    chibis.Add(chibi);
                                }
                            }
                        }
                        else
                        {
                            log.LogWarning($"Chibi {chibi.Name} set to join, but already was present");
                        }
                    }
                    else
                    {
                        try
                        {
                            chibis.Remove(((ChibiScriptParameter)commands[i].Parameters[0]).Chibi);
                        }
                        catch (Exception)
                        {
                            log.LogWarning($"Chibi set to leave was not present.");
                        }
                    }
                }
            }

            int chibiStartX, chibiY;
            if (commands.Any(c => c.Verb == CommandVerb.OP_MODE))
            {
                chibiStartX = 100;
                chibiY = 50;
            }
            else
            {
                chibiStartX = 75;
                chibiY = 100;
            }

            int chibiCurrentX = chibiStartX;
            foreach (ChibiItem chibi in chibis)
            {
                preview.TopScreenChibis.Add(new(chibi, chibiCurrentX, chibiY));
                if (chibiY == 50)
                {
                    chibiY = 100;
                    chibiCurrentX = 75;
                }
                else
                {
                    chibiCurrentX += 30;
                }
            }

            // Draw top screen chibi emotes
            if (currentCommand.Verb == CommandVerb.CHIBI_EMOTE)
            {
                ChibiItem chibi = ((ChibiScriptParameter)currentCommand.Parameters[0]).Chibi;
                if (chibis.Contains(chibi))
                {
                    int chibiIndex = chibis.IndexOf(chibi);
                    int internalYOffset =
                        ((int)((ChibiEmoteScriptParameter)currentCommand.Parameters[1]).Emote - 1) * 32;
                    int externalXOffset = chibiStartX + 30 * chibiIndex;
                    preview.ChibiEmote = (internalYOffset, externalXOffset, chibi);
                }
                else
                {
                    log.LogWarning($"Chibi {chibi.Name} not currently on screen; cannot display emote.");
                }
            }

            // The Haruhi Meter is a bottom screen element but will freeze the game in chess mode, so we can
            // ignore it for the purpose of previews (it will be caught by the validator)
            if (commands.Last().Verb == CommandVerb.HARUHI_METER &&
                ((ShortScriptParameter)commands.Last().Parameters[0]).Value != 0)
            {
                preview.HaruhiMeterVisible = true;
            }
        }
        else
        {
            // Load in the chessboard
            ScriptItemCommand lastChessLoad = commands.LastOrDefault(c => c.Verb == CommandVerb.CHESS_LOAD);
            if (lastChessLoad is not null)
            {
                preview.ChessPuzzle = ((ChessPuzzleScriptParameter)lastChessLoad.Parameters[0]).ChessPuzzle.Clone();
            }

            // Find CHESS_RESET so we can ignore commands before it
            ScriptItemCommand lastChessReset = commands.LastOrDefault(c => c.Verb == CommandVerb.CHESS_RESET);

            // Find chess moves that occurred after last load/reset
            ScriptItemCommand[] chessMoves = commands.Where(c => c.Verb == CommandVerb.CHESS_MOVE
                                                                 && commands.IndexOf(c) > commands.IndexOf(lastChessLoad)
                                                                 && commands.IndexOf(c) > commands.IndexOf(lastChessReset)
                                                                 && commands.IndexOf(c) != commands.Count - 1).ToArray();
            foreach (ScriptItemCommand chessMove in chessMoves)
            {
                int move1StartIndex = ChessPuzzleItem.ConvertSpaceIndexToPieceIndex(((ChessSpaceScriptParameter)chessMove.Parameters[0]).SpaceIndex);
                int move1EndIndex = ChessPuzzleItem.ConvertSpaceIndexToPieceIndex(((ChessSpaceScriptParameter)chessMove.Parameters[1]).SpaceIndex);
                int move2StartIndex = ChessPuzzleItem.ConvertSpaceIndexToPieceIndex(((ChessSpaceScriptParameter)chessMove.Parameters[2]).SpaceIndex);
                int move2EndIndex = ChessPuzzleItem.ConvertSpaceIndexToPieceIndex(((ChessSpaceScriptParameter)chessMove.Parameters[3]).SpaceIndex);

                if (move1StartIndex != move1EndIndex)
                {
                    preview.ChessPuzzle.ChessPuzzle.Chessboard[move1EndIndex] = preview.ChessPuzzle.ChessPuzzle.Chessboard[move1StartIndex];
                    preview.ChessPuzzle.ChessPuzzle.Chessboard[move1StartIndex] = ChessFile.ChessPiece.Empty;
                }

                if (move2StartIndex != move2EndIndex)
                {
                    preview.ChessPuzzle.ChessPuzzle.Chessboard[move2EndIndex] = preview.ChessPuzzle.ChessPuzzle.Chessboard[move2StartIndex];
                    preview.ChessPuzzle.ChessPuzzle.Chessboard[move2StartIndex] = ChessFile.ChessPiece.Empty;
                }
            }

            // Find last CHESS_CLEAR_ANNOTATIONS so we can ignore any annotations after it
            ScriptItemCommand lastChessClearAnnotations = commands.LastOrDefault(c => c.Verb == CommandVerb.CHESS_CLEAR_ANNOTATIONS);

            // Find chess guide commands that occurred after last load/reset
            ScriptItemCommand[] chessGuideCommands = commands.Where(c => c.Verb == CommandVerb.CHESS_TOGGLE_GUIDE
                                                                  && commands.IndexOf(c) > commands.IndexOf(lastChessLoad)
                                                                  && commands.IndexOf(c) > commands.IndexOf(lastChessReset)
                                                                  && commands.IndexOf(c) > commands.IndexOf(lastChessClearAnnotations)
                                                                  && commands.IndexOf(c) != commands.Count - 1).ToArray();

            preview.ChessGuidePieces.Clear();
            foreach (ScriptItemCommand chessGuideCommand in chessGuideCommands)
            {
                ChessSpaceScriptParameter[] chessGuideParams = chessGuideCommand.Parameters[..4].Cast<ChessSpaceScriptParameter>().ToArray();
                if (chessGuideParams.Any(s => s.SpaceIndex > 128))
                {
                    preview.ChessGuidePieces.Clear();
                    continue;
                }
                // Loop through chess guide commands, toggling guide spots on and off
                short[] thisCommandsChessGuides = chessGuideParams
                    .Where(s => s.SpaceIndex != 0 && !preview.ChessGuidePieces.Contains(s.SpaceIndex))
                    .Select(s => s.SpaceIndex).ToArray();
                preview.ChessGuidePieces.Clear();
                preview.ChessGuidePieces.AddRange(thisCommandsChessGuides);
            }

            // Find all highlighted guide spaces
            preview.ChessGuideSpaces.Clear();
            preview.ChessGuideSpaces.AddRange(preview.ChessGuidePieces.SelectMany(g => preview.ChessPuzzle.GetGuideSpaces(g).Select(i => (short)i).Distinct()));

            // Find highlighted spaces
            ScriptItemCommand[] chessHighlightCommands = commands.Where(c => c.Verb == CommandVerb.CHESS_TOGGLE_HIGHLIGHT
                                                                             && commands.IndexOf(c) > commands.IndexOf(lastChessLoad)
                                                                             && commands.IndexOf(c) > commands.IndexOf(lastChessReset)
                                                                             && commands.IndexOf(c) > commands.IndexOf(lastChessClearAnnotations)
                                                                             && commands.IndexOf(c) != commands.Count - 1).ToArray();

            preview.ChessHighlightedSpaces.Clear();
            foreach (ScriptItemCommand chessHighlightCommand in chessHighlightCommands)
            {
                ChessSpaceScriptParameter[] chessHighlightParams = chessHighlightCommand.Parameters.Cast<ChessSpaceScriptParameter>().ToArray();
                // Loop through highlight commands, toggling highlighted spaces on and off
                short[] thisCommandsChessHighlights = chessHighlightParams
                    .Where(s => s.SpaceIndex != 0 && !preview.ChessHighlightedSpaces.Contains(s.SpaceIndex))
                    .Select(s => s.SpaceIndex).ToArray();
                preview.ChessHighlightedSpaces.Clear();
                preview.ChessHighlightedSpaces.AddRange(thisCommandsChessHighlights);
            }

            // Find crossed spaces
            ScriptItemCommand[] chessCrossCommands = commands.Where(c => c.Verb == CommandVerb.CHESS_TOGGLE_CROSS
                                                                             && commands.IndexOf(c) > commands.IndexOf(lastChessLoad)
                                                                             && commands.IndexOf(c) > commands.IndexOf(lastChessReset)
                                                                             && commands.IndexOf(c) > commands.IndexOf(lastChessClearAnnotations)
                                                                             && commands.IndexOf(c) != commands.Count - 1).ToArray();

            preview.ChessCrossedSpaces.Clear();
            foreach (ScriptItemCommand chessCrossCommand in chessCrossCommands)
            {
                ChessSpaceScriptParameter[] chessCrossParams = chessCrossCommand.Parameters.Cast<ChessSpaceScriptParameter>().ToArray();
                // Loop through highlight commands, toggling highlighted spaces on and off
                short[] thisCommandsChessCrosses = chessCrossParams
                    .Where(s => s.SpaceIndex != 0 && !preview.ChessCrossedSpaces.Contains(s.SpaceIndex))
                    .Select(s => s.SpaceIndex).ToArray();
                preview.ChessCrossedSpaces.Clear();
                preview.ChessCrossedSpaces.AddRange(thisCommandsChessCrosses);
            }
        }

        // Draw background
        bool bgReverted = false;
        ScriptItemCommand palCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.PALEFFECT);
        ScriptItemCommand lastBgCommand = commands.LastOrDefault(c => c.Verb is CommandVerb.BG_DISP or CommandVerb.BG_DISP2 or CommandVerb.BG_DISPCG or CommandVerb.BG_FADE or CommandVerb.BG_REVERT);
        if (palCommand is not null && lastBgCommand is not null &&
            commands.IndexOf(palCommand) > commands.IndexOf(lastBgCommand))
        {
            preview.PalEffect = ((PaletteEffectScriptParameter)palCommand.Parameters[0]).Effect;
        }

        if (lastBgCommand == currentCommand && lastBgCommand.Verb == CommandVerb.BG_FADE)
        {
            ScriptItemCommand prevBgCommand = commands[..commands.IndexOf(lastBgCommand)]
                .LastOrDefault(c => c.Verb is CommandVerb.BG_DISP or CommandVerb.BG_DISP2 or CommandVerb.BG_DISPCG or CommandVerb.BG_FADE);
            preview.PrevFadeBackground = prevBgCommand.Verb switch
            {
                CommandVerb.BG_FADE => ((BgScriptParameter)prevBgCommand.Parameters[0]).Background ?? ((BgScriptParameter)prevBgCommand.Parameters[1]).Background,
                _ => ((BgScriptParameter)prevBgCommand.Parameters[0]).Background,
            };
            preview.BgFadeFrames = ((ShortScriptParameter)currentCommand.Parameters[2]).Value;
        }

        ScriptItemCommand bgScrollCommand = null;
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            if (commands[i].Verb == CommandVerb.BG_REVERT)
            {
                bgReverted = true;
                continue;
            }

            if (commands[i].Verb == CommandVerb.BG_SCROLL && bgScrollCommand is null)
            {
                bgScrollCommand = commands[i];
                continue;
            }

            // Checks to see if this is one of the commands that sets a BG_REVERT immune background or if BG_REVERT hasn't been called
            if (commands[i].Verb == CommandVerb.BG_DISP || commands[i].Verb == CommandVerb.BG_DISP2 ||
                (commands[i].Verb == CommandVerb.BG_FADE &&
                 (((BgScriptParameter)commands[i].Parameters[0]).Background is not null)) ||
                (!bgReverted && (commands[i].Verb == CommandVerb.BG_DISPCG ||
                                 commands[i].Verb == CommandVerb.BG_FADE)))
            {
                BackgroundItem background =
                    (commands[i].Verb == CommandVerb.BG_FADE &&
                     ((BgScriptParameter)commands[i].Parameters[0]).Background is null)
                        ? ((BgScriptParameter)commands[i].Parameters[1]).Background
                        : ((BgScriptParameter)commands[i].Parameters[0]).Background;

                if (background is not null)
                {
                    preview.Background = background;
                    preview.BgScrollCommand = bgScrollCommand;
                    if (commands[i].Parameters.Count >= 2 &&
                        commands[i].Parameters[1].Type == ScriptParameter.ParameterType.BOOL)
                    {
                        preview.BgPositionBool = ((BoolScriptParameter)commands[i].Parameters[1]).Value;
                    }

                    break;
                }
            }
        }

        // Draw items
        ScriptItemCommand lastItemCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.ITEM_DISPIMG);
        if (lastItemCommand is not null)
        {
            ItemItem item = (ItemItem)project.Items.FirstOrDefault(i =>
                i.Type == ItemType.Item && ((ItemScriptParameter)lastItemCommand.Parameters[0]).ItemIndex ==
                ((ItemItem)i).ItemIndex);
            if (item is not null)
            {
                ItemItem.ItemTransition transition = lastItemCommand == commands.Last()
                    ? ((ItemTransitionScriptParameter)lastItemCommand.Parameters[2]).Transition
                    : 0;
                preview.Item = (item, ((ItemLocationScriptParameter)lastItemCommand.Parameters[1]).Location, transition);
                if (preview.Item.Location == ItemItem.ItemLocation.Exit && commands.IndexOf(lastItemCommand) == commands.Count - 1)
                {
                    ScriptItemCommand oneBeforeLastItemCommand = commands[..^1].LastOrDefault(c => c.Verb == CommandVerb.ITEM_DISPIMG);
                    preview.ItemPreviousLocation =
                        ((ItemLocationScriptParameter)oneBeforeLastItemCommand?.Parameters[1])?.Location ??
                        ItemItem.ItemLocation.Exit;
                }
            }
        }

        // Draw character sprites
        Dictionary<CharacterItem, PositionedSprite> sprites = [];
        Dictionary<CharacterItem, PositionedSprite> previousSprites = [];

        CharacterItem previousCharacter = null;
        ScriptItemCommand previousCommand = null;
        foreach (ScriptItemCommand command in commands)
        {
            foreach (CharacterItem character in previousSprites.Keys)
            {
                if (SpritePostTransitionScriptParameter.ExitTransitions.Contains(previousSprites[character].PostTransition))
                {
                    sprites.Remove(character);
                    previousSprites.Remove(character);
                }
            }
            if (previousCommand?.Verb == CommandVerb.DIALOGUE) // exits/moves happen _after_ dialogue is advanced, so we check these at this point
            {
                SpritePostTransitionScriptParameter spritePostTransitionMoveParam = (SpritePostTransitionScriptParameter)previousCommand?.Parameters[3];
                if (spritePostTransitionMoveParam.PostTransition != SpritePostTransitionScriptParameter.SpritePostTransition.NO_EXIT)
                {
                    CharacterItem prevCharacter = (CharacterItem)project.Items.First(i =>
                        i.Type == ItemType.Character &&
                        i.Name ==
                        $"CHR_{project.Characters[(int)((DialogueScriptParameter)previousCommand.Parameters[0]).Line.Speaker].Name}");
                    SpriteScriptParameter previousSpriteParam =
                        (SpriteScriptParameter)previousCommand.Parameters[1];
                    short layer = ((ShortScriptParameter)previousCommand.Parameters[9]).Value;
                    switch (spritePostTransitionMoveParam.PostTransition)
                    {
                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_LEFT_FADE_OUT:
                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_RIGHT_FADE_OUT:
                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT:
                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT:
                        case SpritePostTransitionScriptParameter.SpritePostTransition.FADE_OUT_CENTER:
                        case SpritePostTransitionScriptParameter.SpritePostTransition.FADE_OUT_LEFT:
                            if (sprites.ContainsKey(prevCharacter) && previousSprites.ContainsKey(prevCharacter) &&
                                ((SpriteScriptParameter)previousCommand.Parameters[1]).Sprite?.Sprite?.Character ==
                                prevCharacter.MessageInfo.Character && commands.IndexOf(previousCommand) != commands.Count - 2)
                            {
                                sprites.Remove(prevCharacter);
                                previousSprites.Remove(prevCharacter);
                            }

                            break;

                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_CENTER_TO_LEFT_AND_STAY:
                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_RIGHT_TO_LEFT_AND_STAY:
                            sprites[prevCharacter] = new()
                            {
                                Sprite = previousSpriteParam.Sprite,
                                Positioning = new()
                                {
                                    X = SpritePositioning.SpritePosition.LEFT.GetSpriteX(), Layer = layer,
                                },
                            };
                            break;

                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_CENTER_TO_RIGHT_AND_STAY:
                        case SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_LEFT_TO_RIGHT_AND_STAY:
                            sprites[prevCharacter] = new()
                            {
                                Sprite = previousSpriteParam.Sprite,
                                Positioning = new()
                                {
                                    X = SpritePositioning.SpritePosition.RIGHT.GetSpriteX(), Layer = layer,
                                },
                            };
                            break;
                    }

                    // Log sprite exit transitions for animation purposes
                    if (commands.IndexOf(previousCommand) == commands.Count - 2)
                    {
                        sprites[prevCharacter].PostTransition = spritePostTransitionMoveParam.PostTransition;
                    }
                }
            }

            if (command.Verb == CommandVerb.DIALOGUE)
            {
                SpriteScriptParameter spriteParam = (SpriteScriptParameter)command.Parameters[1];
                SKPaint spritePaint = null;
                if (commands.IndexOf(palCommand) > commands.IndexOf(command))
                {
                    spritePaint = ((PaletteEffectScriptParameter)palCommand.Parameters[0]).Effect switch
                    {
                        PaletteEffectScriptParameter.PaletteEffect.PalEffectInverted => PaletteEffectScriptParameter
                            .InvertedPaint,
                        PaletteEffectScriptParameter.PaletteEffect.PalEffectGrayscale => PaletteEffectScriptParameter
                            .GrayscalePaint,
                        PaletteEffectScriptParameter.PaletteEffect.PalEffectSepia => PaletteEffectScriptParameter.SepiaPaint,
                        PaletteEffectScriptParameter.PaletteEffect.PalEffectDimmed => PaletteEffectScriptParameter
                            .DimmedPaint,
                        _ => null,
                    };
                }

                if (spriteParam.Sprite is not null)
                {
                    CharacterItem character;
                    try
                    {
                        character = (CharacterItem)project.Items.First(i =>
                            i.Type == ItemType.Character &&
                            i.DisplayName ==
                            $"CHR_{project.Characters[(int)((DialogueScriptParameter)command.Parameters[0]).Line.Speaker].Name}");
                    }
                    catch (InvalidOperationException)
                    {
                        log.LogWarning(
                            $"Unable to determine speaking character in DIALOGUE command in {DisplayName}.");
                        preview.ErrorImage = "SerialLoops.Graphics.ScriptPreviewError.png";
                        return preview;
                    }

                    SpritePreTransitionScriptParameter spritePreTransitionParam =
                        (SpritePreTransitionScriptParameter)command.Parameters[2];
                    SpriteShakeScriptParameter spriteShakeParam = (SpriteShakeScriptParameter)command.Parameters[4];
                    short layer = ((ShortScriptParameter)command.Parameters[9]).Value;

                    bool spriteIsNew = !sprites.ContainsKey(character);
                    if (spriteIsNew && spritePreTransitionParam.PreTransition !=
                        SpritePreTransitionScriptParameter.SpritePreTransition.NO_TRANSITION)
                    {
                        sprites.Add(character, new());
                        previousSprites.Add(character, new());
                    }

                    if (sprites.TryGetValue(character, out PositionedSprite chrSprite))
                    {
                        previousSprites[character] = chrSprite;
                    }

                    if (spritePreTransitionParam.PreTransition !=
                        SpritePreTransitionScriptParameter.SpritePreTransition.NO_TRANSITION)
                    {
                        switch (spritePreTransitionParam.PreTransition)
                        {
                            // These ones will do their thing no matter what
                            case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_LEFT_TO_CENTER:
                            case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_RIGHT_TO_CENTER:
                                sprites[character] = new()
                                {
                                    Sprite = spriteParam.Sprite,
                                    Positioning =
                                        new()
                                        {
                                            X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(), Layer = layer,
                                        },
                                    PalEffect = spritePaint,
                                };
                                break;

                            case SpritePreTransitionScriptParameter.SpritePreTransition.FADE_TO_CENTER:
                                if (spriteIsNew || previousCharacter != character)
                                {
                                    sprites[character] = new()
                                    {
                                        Sprite = spriteParam.Sprite,
                                        Positioning =
                                            new()
                                            {
                                                X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(),
                                                Layer = layer,
                                            },
                                        PalEffect = spritePaint,
                                    };
                                }

                                break;

                            case SpritePreTransitionScriptParameter.SpritePreTransition.FADE_IN_LEFT:
                            case SpritePreTransitionScriptParameter.SpritePreTransition.PEEK_RIGHT_TO_LEFT:
                            case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_RIGHT_TO_LEFT:
                            case SpritePreTransitionScriptParameter.SpritePreTransition.BOUNCE_RIGHT_TO_LEFT:
                                if (spriteIsNew || previousCharacter != character)
                                {
                                    sprites[character] = new()
                                    {
                                        Sprite = spriteParam.Sprite,
                                        Positioning =
                                            new()
                                            {
                                                X = SpritePositioning.SpritePosition.LEFT.GetSpriteX(),
                                                Layer = layer,
                                            },
                                        PalEffect = spritePaint,
                                    };
                                }
                                break;

                            case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_RIGHT:
                                if (spriteIsNew)
                                {
                                    sprites[character] = new()
                                    {
                                        Sprite = spriteParam.Sprite,
                                        Positioning =
                                            new()
                                            {
                                                X = SpritePositioning.SpritePosition.LEFT.GetSpriteX(),
                                                Layer = layer,
                                            },
                                        PalEffect = spritePaint,
                                    };
                                }
                                else if (previousCharacter != character)
                                {
                                    int startPos = sprites[character].Positioning.X;
                                    sprites[character] = new()
                                    {
                                        Sprite = spriteParam.Sprite,
                                        Positioning =
                                            new()
                                            {
                                                X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(),
                                                Layer = layer,
                                            },
                                        StartPosition = startPos,
                                        PalEffect = spritePaint,
                                    };
                                }

                                break;

                            case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_LEFT_TO_RIGHT:
                            case SpritePreTransitionScriptParameter.SpritePreTransition.BOUNCE_LEFT_TO_RIGHT:
                                if (spriteIsNew || previousCharacter != character)
                                {
                                    sprites[character] = new()
                                    {
                                        Sprite = spriteParam.Sprite,
                                        Positioning =
                                            new()
                                            {
                                                X = SpritePositioning.SpritePosition.RIGHT.GetSpriteX(),
                                                Layer = layer,
                                            },
                                        PalEffect = spritePaint,
                                    };
                                }
                                break;

                            case SpritePreTransitionScriptParameter.SpritePreTransition.SLIDE_LEFT:
                                if (spriteIsNew)
                                {
                                    sprites[character] = new()
                                    {
                                        Sprite = spriteParam.Sprite,
                                        Positioning =
                                            new()
                                            {
                                                X = SpritePositioning.SpritePosition.RIGHT.GetSpriteX(),
                                                Layer = layer,
                                            },
                                        PalEffect = spritePaint,
                                    };
                                }
                                else if (previousCharacter != character)
                                {
                                    int startPos = sprites[character].Positioning.X;
                                    sprites[character] = new()
                                    {
                                        Sprite = spriteParam.Sprite,
                                        Positioning =
                                            new()
                                            {
                                                X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(),
                                                Layer = layer,
                                            },
                                        StartPosition = startPos,
                                        PalEffect = spritePaint,
                                    };
                                }
                                break;
                        }

                        if (commands.IndexOf(command) == commands.Count - 1 && (spriteIsNew || previousCharacter != character))
                        {
                            sprites[character].PreTransition = spritePreTransitionParam.PreTransition;
                        }
                    }
                    else if (sprites.TryGetValue(character, out PositionedSprite sprite))
                    {
                        sprites[character] = new()
                        {
                            Sprite = spriteParam.Sprite,
                            Positioning = sprite.Positioning,
                            PalEffect = spritePaint,
                        };
                    }

                    if (spriteShakeParam.ShakeEffect != SpriteShakeScriptParameter.SpriteShakeEffect.NO_SHAKE &&
                        sprites.ContainsKey(character))
                    {
                        switch (spriteShakeParam.ShakeEffect)
                        {
                            case SpriteShakeScriptParameter.SpriteShakeEffect.SHAKE_LEFT:
                                sprites[character] = new()
                                {
                                    Sprite = spriteParam.Sprite,
                                    Positioning =
                                        new() { X = SpritePositioning.SpritePosition.LEFT.GetSpriteX(), Layer = layer },
                                    PalEffect = spritePaint,
                                };
                                break;

                            case SpriteShakeScriptParameter.SpriteShakeEffect.SHAKE_RIGHT:
                                sprites[character] = new()
                                {
                                    Sprite = spriteParam.Sprite,
                                    Positioning =
                                        new()
                                        {
                                            X = SpritePositioning.SpritePosition.RIGHT.GetSpriteX(), Layer = layer,
                                        },
                                    PalEffect = spritePaint,
                                };
                                break;

                            case SpriteShakeScriptParameter.SpriteShakeEffect.SHAKE_CENTER:
                            case SpriteShakeScriptParameter.SpriteShakeEffect.BOUNCE_HORIZONTAL_CENTER:
                            case SpriteShakeScriptParameter.SpriteShakeEffect
                                .BOUNCE_HORIZONTAL_CENTER_WITH_SMALL_SHAKES:
                                sprites[character] = new()
                                {
                                    Sprite = spriteParam.Sprite,
                                    Positioning =
                                        new()
                                        {
                                            X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(), Layer = layer,
                                        },
                                    PalEffect = spritePaint,
                                };
                                break;
                        }
                    }

                    previousCharacter = character;
                }
            }
            else if (command.Verb == CommandVerb.INVEST_START)
            {
                sprites.Clear();
                previousSprites.Clear();
            }

            previousCommand = command;
        }

        preview.Sprites = [.. sprites.Values.OrderBy(p => p.Positioning.Layer)];

        // Draw dialogue
        ScriptItemCommand lastPinMnlCommand = commands.LastOrDefault(c =>
            c.Verb == CommandVerb.PIN_MNL && c.Section.Equals(commands.Last().Section));
        if (lastPinMnlCommand is not null)
        {
            DialogueLine line = ((DialogueScriptParameter)lastPinMnlCommand.Parameters[0]).Line;
            if (!string.IsNullOrEmpty(line.Text))
            {
                preview.LastDialogueCommand = lastPinMnlCommand;
            }
        }
        else
        {
            ScriptItemCommand lastDialogueCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.DIALOGUE);
            if (commands.FindLastIndex(c => c.Verb == CommandVerb.TOGGLE_DIALOGUE &&
                                            !((BoolScriptParameter)c.Parameters[0]).Value) <
                commands.IndexOf(lastDialogueCommand))
            {
                DialogueLine line = ((DialogueScriptParameter)lastDialogueCommand.Parameters[0]).Line;
                if (!string.IsNullOrEmpty(line.Text))
                {
                    preview.LastDialogueCommand = lastDialogueCommand;
                }
            }
        }

        // Draw the get topic flyout
        if (currentCommand.Verb == CommandVerb.TOPIC_GET)
        {
            preview.Topic = (TopicItem)project.Items.FirstOrDefault(i =>
                i.Type == ItemType.Topic && ((TopicItem)i).TopicEntry.Id ==
                ((TopicScriptParameter)currentCommand.Parameters[0]).TopicId);
        }

        // Draw SELECT choices
        if (currentCommand.Verb == CommandVerb.SELECT)
        {
            preview.CurrentChoices = [];
            if (((OptionScriptParameter)currentCommand.Parameters[0]).Option.Id > 0)
            {
                preview.CurrentChoices.Add(((OptionScriptParameter)currentCommand.Parameters[0]).Option.Text);
            }
            if (((OptionScriptParameter)currentCommand.Parameters[1]).Option.Id > 0)
            {
                preview.CurrentChoices.Add(((OptionScriptParameter)currentCommand.Parameters[1]).Option.Text);
            }
            if (((OptionScriptParameter)currentCommand.Parameters[2]).Option.Id > 0)
            {
                preview.CurrentChoices.Add(((OptionScriptParameter)currentCommand.Parameters[2]).Option.Text);
            }
            if (((OptionScriptParameter)currentCommand.Parameters[3]).Option.Id > 0)
            {
                preview.CurrentChoices.Add(((OptionScriptParameter)currentCommand.Parameters[3]).Option.Text);
            }
        }

        return preview;
    }

    public static (SKBitmap PreviewImage, string ErrorImage) GeneratePreviewImage(ScriptPreview preview,
        Project project)
    {
        SKBitmap previewBitmap = new(256, 384);
        using SKCanvas canvas = new(previewBitmap);
        canvas.DrawColor(SKColors.Black);

        if (!string.IsNullOrEmpty(preview.ErrorImage))
        {
            return (null, preview.ErrorImage);
        }

        int verticalOffset = preview.ChessMode ? 0 : 192;

        if (!preview.ChessMode)
        {
            if (preview.EpisodeHeader != 0)
            {
                canvas.DrawBitmap(
                    EpisodeHeaderScriptParameter
                        .GetTexture((EpisodeHeaderScriptParameter.Episode)preview.EpisodeHeader, project).GetTexture(),
                    new SKPoint(0, 0));
            }
            else
            {
                if (preview.Kbg is not null)
                {
                    canvas.DrawBitmap(preview.Kbg.GetBackground(), new SKPoint(0, 0));
                }

                if (preview.Place is not null)
                {
                    canvas.DrawBitmap(preview.Place.GetPreview(project), new SKPoint(5, 40));
                }

                foreach (var chibi in preview.TopScreenChibis)
                {
                    SKBitmap chibiFrame = chibi.Chibi.ChibiAnimations.First().Value.ElementAt(0).Frame;
                    canvas.DrawBitmap(chibiFrame, new SKPoint(chibi.X, chibi.Y));
                }

                if (preview.ChibiEmote.EmotingChibi is not null)
                {
                    SKBitmap emotes = project.Grp.GetFileByName("SYS_ADV_T08DNX")
                        .GetImage(width: 32, transparentIndex: 0);
                    int chibiY = preview.TopScreenChibis.First(c => c.Chibi == preview.ChibiEmote.EmotingChibi).Y;
                    canvas.DrawBitmap(emotes,
                        new(0, preview.ChibiEmote.InternalYOffset, 32, preview.ChibiEmote.InternalYOffset + 32),
                        new SKRect(preview.ChibiEmote.ExternalXOffset + 16, chibiY - 32,
                            preview.ChibiEmote.ExternalXOffset + 48, chibiY));
                }
            }
        }
        else if (preview.ChessPuzzle is not null)
        {
            canvas.DrawBitmap(preview.ChessPuzzle.GetChessboard(project), 8, 188);

            foreach (SKPoint rectOrigin in preview.ChessGuideSpaces.Select(g => ChessPuzzleItem.GetChessPiecePosition(g)))
            {
                canvas.DrawRect(rectOrigin.X + 5, rectOrigin.Y + 203, 20, 19, new() { Color = SKColors.DarkRed.WithAlpha(128) });
            }

            foreach (SKPoint rectOrigin in preview.ChessHighlightedSpaces.Select(h => ChessPuzzleItem.GetChessSpacePosition(h)))
            {
                canvas.DrawRect(rectOrigin.X + 5, rectOrigin.Y + 203, 20, 19, new() { Color = SKColors.Gold.WithAlpha(128) });
            }

            foreach (SKPoint rectOrigin in preview.ChessCrossedSpaces.Select(c => ChessPuzzleItem.GetChessSpacePosition(c)))
            {
                canvas.DrawRect(rectOrigin.X + 5, rectOrigin.Y + 203, 20, 19, new() { Color = SKColors.Purple.WithAlpha(128) });
            }
        }

        // Draw background
        if (preview.Background is not null)
        {
            switch (preview.Background.BackgroundType)
            {
                case BgType.TEX_CG_DUAL_SCREEN:
                    SKBitmap dualScreenBg = preview.Background.GetBackground();
                    if (preview.BgScrollCommand is not null &&
                        ((BgScrollDirectionScriptParameter)preview.BgScrollCommand.Parameters[0]).ScrollDirection ==
                        BgScrollDirectionScriptParameter.BgScrollDirection.BgScrollDown)
                    {
                        canvas.DrawBitmap(dualScreenBg,
                            new(0, preview.Background.Graphic2.Height - 192, 256,
                                preview.Background.Graphic2.Height), new SKRect(0, 0, 256, 192));
                        int bottomScreenX = dualScreenBg.Height - 192;
                        canvas.DrawBitmap(dualScreenBg, new(0, bottomScreenX, 256, bottomScreenX + 192),
                            new SKRect(0, 192, 256, 384));
                    }
                    else
                    {
                        canvas.DrawBitmap(dualScreenBg, new(0, 0, 256, 192), new SKRect(0, 0, 256, 192));
                        canvas.DrawBitmap(dualScreenBg,
                            new(0, preview.Background.Graphic2.Height, 256,
                                preview.Background.Graphic2.Height + 192), new SKRect(0, 192, 256, 384));
                    }

                    break;

                case BgType.TEX_CG_SINGLE:
                    if (preview.BgPositionBool || (preview.BgScrollCommand is not null &&
                                                   ((BgScrollDirectionScriptParameter)preview.BgScrollCommand
                                                       .Parameters[0]).ScrollDirection ==
                                                   BgScrollDirectionScriptParameter.BgScrollDirection.BgScrollDown))
                    {
                        SKBitmap bgBitmap = preview.Background.GetBackground();
                        canvas.DrawBitmap(bgBitmap,
                            new(0, bgBitmap.Height - 192, bgBitmap.Width, bgBitmap.Height),
                            new SKRect(0, verticalOffset, 256, verticalOffset + 192));
                    }
                    else
                    {
                        canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, verticalOffset));
                    }

                    break;

                case BgType.TEX_CG_WIDE:
                    if (preview.BgScrollCommand is not null &&
                        ((BgScrollDirectionScriptParameter)preview.BgScrollCommand.Parameters[0]).ScrollDirection ==
                        BgScrollDirectionScriptParameter.BgScrollDirection.BgScrollRight)
                    {
                        SKBitmap bgBitmap = preview.Background.GetBackground();
                        canvas.DrawBitmap(bgBitmap, new(bgBitmap.Width - 256, 0, bgBitmap.Width, 192),
                            new SKRect(0, verticalOffset, 256, verticalOffset + 192));
                    }
                    else
                    {
                        canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, verticalOffset));
                    }

                    break;

                case BgType.TEX_CG:
                    canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, verticalOffset));
                    break;

                default:
                    canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, verticalOffset),
                        PaletteEffectScriptParameter.GetPaletteEffectPaint(preview.PalEffect));
                    break;
            }
        }

        if (preview.Item.Item is not null)
        {
            int width = preview.Item.Item.ItemGraphic.Width;
            switch (preview.Item.Location)
            {
                case ItemItem.ItemLocation.Left:
                    canvas.DrawBitmap(preview.Item.Item.ItemGraphic.GetImage(transparentIndex: 0), 128 - width,
                        verticalOffset + 12);
                    break;

                case ItemItem.ItemLocation.Center:
                    canvas.DrawBitmap(preview.Item.Item.ItemGraphic.GetImage(transparentIndex: 0), 128 - width / 2,
                        verticalOffset + 12);
                    break;

                case ItemItem.ItemLocation.Right:
                    canvas.DrawBitmap(preview.Item.Item.ItemGraphic.GetImage(transparentIndex: 0), 128, verticalOffset + 12);
                    break;

                default:
                case ItemItem.ItemLocation.Exit:
                    break;
            }
        }

        // Draw character sprites
        foreach (PositionedSprite sprite in preview.Sprites)
        {
            if (sprite.Sprite is not null)
            {
                SKBitmap spriteBitmap = sprite.Sprite.GetClosedMouthAnimation(project)[0].Frame;
                canvas.DrawBitmap(spriteBitmap, sprite.Positioning.GetSpritePosition(spriteBitmap, verticalOffset),
                    sprite.PalEffect ?? PaletteEffectScriptParameter.IdentityPaint);
            }
        }

        // Draw dialogue
        if (preview.LastDialogueCommand is not null)
        {
            DialogueLine line = ((DialogueScriptParameter)preview.LastDialogueCommand.Parameters[0]).Line;
            SKPaint dialoguePaint = preview.LastDialogueCommand.Verb == CommandVerb.PIN_MNL
                ? project.DialogueColorFilters[1]
                : line.Speaker switch
                {
                    Speaker.MONOLOGUE => project.DialogueColorFilters[1],
                    Speaker.INFO => project.DialogueColorFilters[4],
                    _ => project.DialogueColorFilters[0],
                };
            if (!string.IsNullOrEmpty(line.Text))
            {
                canvas.DrawBitmap(project.DialogueBitmap, new(0, 24, 32, 36), new SKRect(0, verticalOffset + 152, 256, verticalOffset + 164));
                SKColor dialogueBoxColor = project.DialogueBitmap.GetPixel(0, 28);
                canvas.DrawRect(0, verticalOffset + 164, 256, 28, new() { Color = dialogueBoxColor });
                canvas.DrawBitmap(project.DialogueBitmap, new(0, 37, 32, 64),
                    new SKRect(224, verticalOffset + 165, 256, verticalOffset + 192));
                if (preview.LastDialogueCommand.Verb != CommandVerb.PIN_MNL)
                {
                    canvas.DrawBitmap(project.SpeakerBitmap,
                        new(0, 16 * ((int)line.Speaker - 1), 64, 16 * ((int)line.Speaker)),
                        new SKRect(0, verticalOffset + 140, 64, verticalOffset + 156));
                }

                canvas.DrawHaroohieText(line.Text, dialoguePaint, project, y: verticalOffset + 160);
            }
        }

        // Draw topic obtained flyout
        if (preview.Topic is not null)
        {
            SKBitmap flyoutSysTex = project.Grp.GetFileByName("SYS_ADV_B01DNX").GetImage(transparentIndex: 0);
            SKBitmap topicFlyout = new(76, 32);
            using SKCanvas flyoutCanvas = new(topicFlyout);

            flyoutCanvas.DrawBitmap(flyoutSysTex, new(0, 20, 32, 32),
                new SKRect(0, 12, 32, 24));
            flyoutCanvas.DrawBitmap(flyoutSysTex, new(0, 0, 44, 20),
                new SKRect(32, 6, 76, 26));

            SKBitmap topicCards = project.Grp.GetFileByName("SYS_CMN_B09DNX").GetImage(transparentIndex: 0);
            int srcX = preview.Topic.TopicEntry.CardType switch
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

            canvas.DrawBitmap(topicFlyout, 256 - topicFlyout.Width, verticalOffset + 128);
        }

        // Draw select choices
        if (preview.CurrentChoices?.Count > 0)
        {
            List<SKBitmap> choiceGraphics = [];
            foreach (string choice in preview.CurrentChoices)
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
                canvas.DrawBitmap(choiceGraphic, 19, graphicY);
                graphicY += 26;
            }
        }

        if (preview.HaruhiMeterVisible)
        {
            SKBitmap haruhiMeterBitmap = ((SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_B14")).GetTexture();
            canvas.DrawBitmap(haruhiMeterBitmap, 0, 192);
        }

        canvas.Flush();
        return (previewBitmap, null);
    }

    public (SKBitmap PreviewImage, string ErrorImage) GeneratePreviewImage(
        OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commandTree, ScriptItemCommand currentCommand,
        Project project, ILogger log)
    {
        return GeneratePreviewImage(GetScriptPreview(commandTree, currentCommand, project, log), project);
    }

    public void UpdateEventTableInfo(EventTable evtTbl)
    {
        EventTableEntry entry = evtTbl.Entries.FirstOrDefault(e => e.EventFileIndex == Event.Index);
        if (entry is not null)
        {
            StartReadFlag = entry.FirstReadFlag;
            SfxGroupIndex = entry.SfxGroupIndex;
        }
        else
        {
            StartReadFlag = -1;
            SfxGroupIndex = -1;
        }
    }

    private void PruneLabelsSection(ILogger log)
    {
        if ((Event.LabelsSection?.Objects?.Count ?? 0) - 1 > Event.ScriptSections.Count)
        {
            try
            {
                for (int i = 0; i < Event.LabelsSection.Objects.Count; i++)
                {
                    if (Event.LabelsSection.Objects[i].Id == 0)
                    {
                        continue;
                    }

                    if (!Event.ScriptSections.Select(s => s.Name)
                            .Contains(Event.LabelsSection.Objects[i].Name.Replace("/", "")))
                    {
                        Event.LabelsSection.Objects.RemoveAt(i);
                        i--;
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogException(_localize("ErrorScriptItemPruningLabels"), ex);
                log.LogWarning($"Script: {Name}, DisplayName: {DisplayName}");
            }
        }
    }

    public override void Refresh(Project project, ILogger log)
    {
        Graph = new();
        Graph.AddVertexRange(Event.ScriptSections);
        CalculateGraphEdges(GetScriptCommandTree(project, log), log);
    }

    public override string ToString() => DisplayName;
}
