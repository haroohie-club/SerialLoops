using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;
using SoftCircuits.Collections;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;
using static SerialLoops.Lib.Script.SpritePositioning;

namespace SerialLoops.Lib.Util;

public static partial class Extensions
{
    public static SKSamplingOptions HighQualitySamplingOptions => new(SKFilterMode.Linear, SKMipmapMode.Linear);

    public static int GetSpriteX(this SpritePosition position)
    {
        return position switch
        {
            SpritePosition.LEFT => -64,
            SpritePosition.CENTER => 0,
            SpritePosition.RIGHT => 64,
            _ => 0,
        };
    }

    public static string GetGraphicInfoFile(this GraphicsFile grp)
    {
        return JsonSerializer.Serialize(new GraphicInfo(grp));
    }

    public static float GetMaxAmplitude(this IWaveProvider waveProvider, ILogger log)
    {
        return waveProvider.ToSampleProvider().GetMaxAmplitude(log);
    }

    public static float GetMaxAmplitude(this ISampleProvider sampleProvider, ILogger log)
    {
        float max = 0;
        float[] buffer = new float[sampleProvider.WaveFormat.SampleRate];
        int read;
        do
        {
            try
            {
                read = sampleProvider.Read(buffer, 0, buffer.Length);
                max = Math.Max(max, buffer.Max());
            }
            catch (Exception ex)
            {
                read = 0;
                log.LogWarning($"Failed to read all samples during max amplitude calculation: {ex.Message}");
            }
        } while (read > 0);

        return max;
    }

    public static string GetSubstitutedString(this string line, Project project)
    {
        if (project.LangCode != "ja")
        {
            // we replace " in the base library, but we don't want to do that here since we'll rely on rich-text editing instead
            return string.Join("",
                line.Select(c => c != '“' ? (project.FontReplacement.ReverseLookup(c)?.ReplacedCharacter ?? c) : c));
        }
        else
        {
            return line;
        }
    }

    public static string GetOriginalString(this string line, Project project)
    {
        if (project.LangCode != "ja")
        {
            string originalString = string.Join("",
                line.Select(c =>
                    project.FontReplacement.ContainsKey(c) ? project.FontReplacement[c].OriginalCharacter : c));
            foreach (Match match in Regex.Matches(originalString, @"\$(\d{1,2})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }

            foreach (Match match in Regex.Matches(originalString, @"。Ｗ(\d{1,2})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }

            foreach (Match match in Regex.Matches(originalString, @"。Ｐ(\d{2})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }

            foreach (Match match in Regex.Matches(originalString, @"。Ｑ(\d{2})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }

            foreach (Match match in Regex.Matches(originalString, @"。ｘ(\d{2})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }
            foreach (Match match in Regex.Matches(originalString, @"。ｙ(\d{2})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }
            originalString = Regex.Replace(originalString, @"。Ｘ", "#X");
            foreach (Match match in Regex.Matches(originalString, @"。Ｙ(\d{1,2})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }

            originalString = Regex.Replace(originalString, @"。ＤＰ", "#DP");
            foreach (Match match in Regex.Matches(originalString, @"。ＳＥ(\d{3})").Cast<Match>())
            {
                originalString = originalString.Replace(match.Value, match.Value.GetSubstitutedString(project));
            }

            originalString = Regex.Replace(originalString, @"。ＳＫ０", "#SK0");
            originalString = Regex.Replace(originalString, @"。ｓｋ", "#sk");
            return originalString;
        }
        else
        {
            return line;
        }
    }

    public static void InitializeWithDefaultValues(this ScriptCommandInvocation invocation, EventFile eventFile, Project project)
    {
        switch (Enum.Parse<CommandVerb>(invocation.Command.Mnemonic))
        {
            case CommandVerb.BG_DISP:
            case CommandVerb.BG_DISP2:
                invocation.Parameters[0] = (short)((BackgroundItem)project.Items.First(i =>
                    i.Type == ItemDescription.ItemType.Background &&
                    ((BackgroundItem)i).BackgroundType == BgType.TEX_BG)).Id;
                break;

            case CommandVerb.BG_DISPCG:
                invocation.Parameters[0] = (short)((BackgroundItem)project.Items.First(i =>
                    i.Type == ItemDescription.ItemType.Background &&
                    ((BackgroundItem)i).BackgroundType != BgType.KINETIC_SCREEN &&
                    ((BackgroundItem)i).BackgroundType != BgType.TEX_BG)).Id;
                break;

            case CommandVerb.BG_SCROLL:
                invocation.Parameters[1] = 1;
                break;

            case CommandVerb.BGM_PLAY:
                invocation.Parameters[1] = (short)BgmModeScriptParameter.BgmMode.Start;
                invocation.Parameters[2] = 100;
                break;

            case CommandVerb.CHIBI_ENTEREXIT:
            case CommandVerb.CHIBI_EMOTE:
                invocation.Parameters[0] =
                    (short)((ChibiItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi))
                    .TopScreenIndex;
                break;

            case CommandVerb.DIALOGUE:
                invocation.Parameters[0] = (short)(eventFile.DialogueSection.Objects.Count);
                DialogueLine line = new(project.Localize("Replace me").GetOriginalString(project), eventFile);
                eventFile.DialogueSection.Objects.Add(line);
                invocation.Parameters[6] = (short)project.MessInfo.MessageInfos.FindIndex(i => i.Character == line.Speaker);
                invocation.Parameters[7] = (short)project.MessInfo.MessageInfos.FindIndex(i => i.Character == line.Speaker);
                break;

            case CommandVerb.FLAG:
                invocation.Parameters[0] = 1;
                break;

            case CommandVerb.GOTO:
                invocation.Parameters[0] = eventFile.LabelsSection.Objects.First(l => l.Id > 0).Id;
                break;

            case CommandVerb.KBG_DISP:
                invocation.Parameters[0] = (short)((BackgroundItem)project.Items.First(i =>
                    i.Type == ItemDescription.ItemType.Background &&
                    ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN)).Id;
                break;

            case CommandVerb.LOAD_ISOMAP:
                invocation.Parameters[0] =
                    (short)((MapItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Map)).Map.Index;
                break;

            case CommandVerb.INVEST_START:
                invocation.Parameters[4] = eventFile.LabelsSection.Objects.First(l => l.Id > 0).Id;
                break;

            case CommandVerb.MODIFY_FRIENDSHIP:
                invocation.Parameters[0] = 2;
                break;

            case CommandVerb.PIN_MNL:
                invocation.Parameters[0] = (short)(eventFile.DialogueSection.Objects.Count);
                DialogueLine mnl = new(project.Localize("Replace me").GetOriginalString(project), eventFile);
                eventFile.DialogueSection.Objects.Add(mnl);
                break;

            case CommandVerb.SCENE_GOTO:
            case CommandVerb.SCENE_GOTO_CHESS:
                invocation.Parameters[0] = (short)eventFile.ConditionalsSection.Objects.Count;
                eventFile.ConditionalsSection.Objects.Add(string.Empty);
                break;

            case CommandVerb.SCREEN_FADEOUT:
                invocation.Parameters[1] = 100;
                break;

            case CommandVerb.SCREEN_FLASH:
                invocation.Parameters[0] = 5;
                invocation.Parameters[1] = 30;
                invocation.Parameters[2] = 5;
                break;

            case CommandVerb.SND_PLAY:
                invocation.Parameters[1] = (short)SfxModeScriptParameter.SfxMode.Start;
                invocation.Parameters[2] = 100;
                invocation.Parameters[3] = -1;
                invocation.Parameters[4] = -1;
                break;

            case CommandVerb.SELECT:
                invocation.Parameters[4] = -1;
                invocation.Parameters[5] = -1;
                invocation.Parameters[6] = -1;
                invocation.Parameters[7] = -1;
                break;

            case CommandVerb.VCE_PLAY:
                invocation.Parameters[0] =
                    (short)((VoicedLineItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Voice)).Index;
                break;

            case CommandVerb.VGOTO:
                invocation.Parameters[0] = (short)eventFile.ConditionalsSection.Objects.Count;
                eventFile.ConditionalsSection.Objects.Add(string.Empty);
                invocation.Parameters[2] = eventFile.LabelsSection.Objects.FirstOrDefault(l => l.Id > 0)?.Id ?? 0;
                break;
        }
    }

    public static ScriptCommandInvocation Clone(this ScriptCommandInvocation invocation, EventFile eventFile, Project project)
    {
        ScriptCommandInvocation clonedInvocation = new(invocation.Command);
        clonedInvocation.InitializeWithDefaultValues(eventFile, project);
        return clonedInvocation;
    }

    public static void ApplyScriptPreview(this QuickSaveSlotData quickSave, ScriptPreview scriptPreview, ScriptItem script, int commandIndex, Project project, ILogger log)
    {
        quickSave.KbgIndex = (short)(scriptPreview.Kbg?.Id ?? ((BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN)).Id);
        quickSave.BgmIndex = (short)(scriptPreview.Bgm?.Index ?? -1);
        quickSave.Place = (short)(scriptPreview.Place?.Index ?? 0);
        if (scriptPreview.Background.BackgroundType == BgType.TEX_BG)
        {
            quickSave.BgIndex = (short)scriptPreview.Background.Id;
            quickSave.CgIndex = 0;
        }
        else
        {
            OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commandTree = script.GetScriptCommandTree(project, log);
            ScriptItemCommand currentCommand = commandTree[script.Event.ScriptSections[quickSave.CurrentScriptBlock]][commandIndex];
            List<ScriptItemCommand> commands = currentCommand.WalkCommandGraph(commandTree, script.Graph);
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Verb == CommandVerb.BG_DISP || commands[i].Verb == CommandVerb.BG_DISP2 || (commands[i].Verb == CommandVerb.BG_FADE && ((BgScriptParameter)commands[i].Parameters[0]).Background is not null))
                {
                    quickSave.BgIndex = (short)((BgScriptParameter)commands[i].Parameters[0]).Background.Id;
                }
            }
            quickSave.CgIndex = (short)scriptPreview.Background.Id;
        }
        quickSave.BgPalEffect = (short)scriptPreview.PalEffect;
        quickSave.EpisodeHeader = scriptPreview.EpisodeHeader;
        for (int i = 1; i <= 5; i++)
        {
            if (scriptPreview.TopScreenChibis.Any(c => c.Chibi.TopScreenIndex == i))
            {
                quickSave.TopScreenChibis |= (CharacterMask)(1 << i);
            }
        }
        quickSave.FirstCharacterSprite = scriptPreview.Sprites.ElementAtOrDefault(0)?.Sprite?.Index ?? 0;
        quickSave.SecondCharacterSprite = scriptPreview.Sprites.ElementAtOrDefault(1)?.Sprite?.Index ?? 0;
        quickSave.ThirdCharacterSprite = scriptPreview.Sprites.ElementAtOrDefault(2)?.Sprite?.Index ?? 0;
        quickSave.Sprite1XOffset = (short)(scriptPreview.Sprites.ElementAtOrDefault(0)?.Positioning?.X ?? 0);
        quickSave.Sprite2XOffset = (short)(scriptPreview.Sprites.ElementAtOrDefault(1)?.Positioning?.X ?? 0);
        quickSave.Sprite3XOffset = (short)(scriptPreview.Sprites.ElementAtOrDefault(2)?.Positioning?.X ?? 0);
    }

    public static void CollectGarbage(this EventFile evt)
    {
        // Collect conditional garbage
        IEnumerable<string> conditionalContainingCommands =
            new[] { CommandVerb.SELECT, CommandVerb.VGOTO, CommandVerb.SCENE_GOTO, CommandVerb.SCENE_GOTO_CHESS }.Select(c => c.ToString());
        List<UsedIndex> conditionalUsedIndices = [];
        foreach (ScriptCommandInvocation conditionalCommand in evt.ScriptSections.SelectMany(s => s.Objects)
                     .Where(c => conditionalContainingCommands.Contains(c.Command.Mnemonic)))
        {
            if (conditionalCommand.Command.Mnemonic == CommandVerb.SELECT.ToString())
            {
                conditionalUsedIndices.AddRange(conditionalCommand.Parameters[4..8].Where(p => p >= 0)
                    .Select((p, i) => new UsedIndex { Command = conditionalCommand, ConditionalIndex = p, ParameterIndex = i + 4 }));
            }
            conditionalUsedIndices.Add(new() { Command = conditionalCommand, ConditionalIndex = conditionalCommand.Parameters[0], ParameterIndex = 0});
        }

        if (conditionalUsedIndices.DistinctBy(c => c.ConditionalIndex).Count() < evt.ConditionalsSection.Objects.Count)
        {
            List<int> indicesForDeletion = [];
            for (short i = 0; i < evt.ConditionalsSection.Objects.Count; i++)
            {
                if (evt.ConditionalsSection.Objects[i] is null)
                {
                    continue;
                }
                if (!conditionalUsedIndices.Select(idx => idx.ConditionalIndex).Contains(i))
                {
                    indicesForDeletion.Add(i);
                }
            }

            for (int i = 0; i < indicesForDeletion.Count; i++)
            {
                evt.ConditionalsSection.Objects.RemoveAt(indicesForDeletion[i]);
                for (int j = 0; j < conditionalUsedIndices.Count; j++)
                {
                    if (conditionalUsedIndices[j].ConditionalIndex >= indicesForDeletion[i])
                    {
                        conditionalUsedIndices[j].Command.Parameters[conditionalUsedIndices[j].ParameterIndex]--;
                        conditionalUsedIndices[j].ConditionalIndex--;
                    }
                }

                for (int k = i + 1; k < indicesForDeletion.Count; k++)
                {
                    indicesForDeletion[k]--;
                }
            }
        }

        // Collect dialogue garbage
        // IEnumerable<string> dialogueContainingCommands = new[] { CommandVerb.DIALOGUE, CommandVerb.PIN_MNL }.Select(c => c.ToString());
        // List<UsedIndex> dialogueUsedIndices = [];
        // foreach (ScriptCommandInvocation dialogueCommand in evt.ScriptSections.SelectMany(s => s.Objects)
        //              .Where((c => dialogueContainingCommands.Contains(c.Command.Mnemonic))))
        // {
        //     dialogueUsedIndices.Add(new() { Command = dialogueCommand, Index = dialogueCommand.Parameters[0] });
        // }
        //
        // if (dialogueUsedIndices.DistinctBy(i => i.Index).Count() < evt.DialogueSection.Objects.Count)
        // {
        //     for (short i = 0; i < evt.DialogueSection.Objects.Count; i++)
        //     {
        //         if (dialogueUsedIndices.All(idx => idx.Index != i))
        //         {
        //             evt.DialogueSection.Objects.RemoveAt(i);
        //             evt.DialogueLines.RemoveAt(i--);
        //             for (int j = 0; j < dialogueUsedIndices.Count; j++)
        //             {
        //                 if (dialogueUsedIndices[j].Index >= i)
        //                 {
        //                     dialogueUsedIndices[j].Command.Parameters[0]--;
        //                     dialogueUsedIndices[j].Index--;
        //                 }
        //             }
        //         }
        //     }
        // }
    }

    private class UsedIndex
    {
        public ScriptCommandInvocation Command { get; set; }
        public short ConditionalIndex { get; set; }
        public int ParameterIndex { get; set; }
    }

    public static SKPaint GetColorFilter(this SKColor color)
    {
        return new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(
                [color.Red / 256f, 0f,                 0f,                0f, 0f,
                    0f,                color.Green / 256f, 0f,                0f, 0f,
                    0f,                0f,                 color.Blue / 256f, 0f, 0f,
                    0f,                0f,                 0f,                1f, 0f]
            ),
        };
    }

    public static int CalculateHaroohieTextWidthReplaced(this string str, Project project)
    {
        return str.CalculateHaroohieTextWidth(project, replaced: true);
    }

    public static int CalculateHaroohieTextWidth(this string str, Project project, bool replaced = false)
    {
        if (project.LangCode.Equals("ja"))
        {
            return str.Length * 14;
        }
        int strWidth = 0;
        for (int i = 0; i < str.Length; i++)
        {
            FontReplacement fr = replaced ? project.FontReplacement[str[i]] : project.FontReplacement.ReverseLookup(str[i]);
            if ((fr?.CauseOffsetAdjust ?? false) && i < str.Length - 1)
            {
                FontReplacement nextFr = replaced ? project.FontReplacement[str[i + 1]] : project.FontReplacement.ReverseLookup(str[i + 1]);
                if (nextFr?.TakeOffsetAdjust ?? false)
                {
                    strWidth += fr.Offset - 1;
                }
                else
                {
                    strWidth += fr.Offset;
                }
            }
            else
            {
                strWidth += fr?.Offset ?? 15;
            }
        }
        return strWidth;
    }

    public static void DrawHaroohieText(this SKCanvas canvas, string text, SKPaint color, Project project, int x = 10,
        int y = 352, bool formatting = true)
    {
        int currentX = x;
        int currentY = y;
        int manualX = -1;
        int manualY = -1;
        int manualIndent = 0;

        for (int i = 0; i < text.Length; i++)
        {
            // handle newlines
            if (text[i] == '\n' || (i < text.Length - 2 && text[i..(i + 2)] == "#n"))
            {
                currentX = 10 + manualIndent;
                currentY += manualY >= 0 ? manualY : 14;
                if (text[i] == '#')
                {
                    i++;
                }
                continue;
            }

            // handle operators
            if (formatting)
            {
                if (i < text.Length - 2 && Regex.IsMatch(text[i..(i + 2)], @"\$\d"))
                {
                    if (i < text.Length - 3 && Regex.IsMatch(text[i..(i + 3)], @"\$\d{2}"))
                    {
                        i++;
                    }

                    i++;
                    continue;
                }
                else if (i < text.Length - 3 && Regex.IsMatch(text[i..(i + 3)], @"#W\d"))
                {
                    if (i < text.Length - 4 && Regex.IsMatch(text[i..(i + 4)], @"#W\d{2}"))
                    {
                        i++;
                    }

                    i += 2;
                    continue;
                }
                else if (i < text.Length - 4 && Regex.IsMatch(text[i..(i + 4)], @"#P\d{2}"))
                {
                    color = project.DialogueColorFilters.ElementAtOrDefault(int.Parse(Regex.Match(text[i..(i + 4)], @"#P(?<id>\d{2})").Groups["id"].Value));
                    i += 3;
                    continue;
                }
                else if (i < text.Length - 4 && ManualXRegex().IsMatch(text[i..(i + 4)]))
                {
                    manualX = int.Parse(ManualXRegex().Match(text[i..(i + 4)]).Groups["offset"].Value) + 14;
                    i += 3;
                    continue;
                }
                else if (i < text.Length - 4 && ManualYRegex().IsMatch(text[i..(i + 4)]))
                {
                    manualY = int.Parse(ManualYRegex().Match(text[i..(i + 4)]).Groups["offset"].Value) + 14;
                    i += 3;
                    continue;
                }
                else if (i < text.Length - 2 && text[i..(i + 2)] == "#X")
                {
                    manualIndent = currentX;
                    i++;
                    continue;
                }
                else if (i < text.Length - 3 && text[i..(i + 3)] == "#DP")
                {
                    i += 3;
                }
                else if (i < text.Length - 6 && Regex.IsMatch(text[i..(i + 6)], @"#SE\d{3}"))
                {
                    i += 6;
                }
                else if (i < text.Length - 6 && Regex.IsMatch(text[i..(i + 4)], @"#Q\d{2}"))
                {
                    i += 4;
                }
                else if (i < text.Length - 4 && text[i..(i + 4)] == "#SK0")
                {
                    i += 4;
                }
                else if (i < text.Length - 3 && text[i..(i + 3)] == "#sk")
                {
                    i += 3;
                }
            }

            if (text[i] != '　') // if it's a space, we just skip drawing
            {
                int charIndex = project.FontMap.CharMap.IndexOf(text[i]);
                if ((charIndex + 1) * 16 <= project.FontBitmap.Height)
                {
                    canvas.DrawBitmap(project.FontBitmap, new(0, charIndex * 16, 16, (charIndex + 1) * 16),
                        new SKRect(currentX, currentY, currentX + 16, currentY + 16), color);
                }
            }

            if (manualX >= 0)
            {
                currentX += manualX;
            }
            else
            {
                FontReplacement replacement = project.FontReplacement.ReverseLookup(text[i]);
                if (replacement is not null && project.LangCode != "ja")
                {
                    if (replacement.CauseOffsetAdjust && i < text.Length - 1)
                    {
                        project.FontReplacement.TryGetValue(text[i + 1], out FontReplacement nextFr);
                        if (nextFr?.TakeOffsetAdjust ?? false)
                        {
                            currentX += replacement.Offset - 1;
                        }
                        else
                        {
                            currentX += replacement.Offset;
                        }
                    }
                    else
                    {
                        currentX += replacement.Offset;
                    }
                }
                else
                {
                    currentX += 14;
                }
            }
        }
    }

    public static string GetLocalizedFilePath(string path, string extension)
    {
        string cultureName = $"{path}.{CultureInfo.CurrentCulture.Name}.{extension}";
        string languageName = $"{path}.{CultureInfo.CurrentCulture.TwoLetterISOLanguageName}.{extension}";
        if (File.Exists(cultureName))
        {
            return cultureName;
        }
        else if (File.Exists(languageName))
        {
            return languageName;
        }
        else
        {
            string enUSName = $"{path}.en-US.{extension}";
            string enLangName = $"{path}.en.{extension}";
            if (File.Exists(enUSName))
            {
                return enUSName;
            }
            else if (File.Exists(enLangName))
            {
                return enLangName;
            }
            else if (File.Exists($"{path}.{extension}"))
            {
                return $"{path}.{extension}";
            }
        }

        return string.Empty;
    }

    public static string ToSentenceCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }
        else if (str.Length == 1)
        {
            return str.ToUpper();
        }

        return $"{str[0].ToString().ToUpper()}{str[1..]}";
    }

    // We need this stupid method for project export because Path.Combine
    // on Windows it results in inserting filenames with \ instead of /
    // which then breaks extraction for those archives on Unix
    // i hate it lol
    public static string ToUnixPath(this string str)
    {
        return str.Replace('\\', '/');
    }

    public static bool Swap<T, TS>(this OrderedDictionary<T, TS> orderedDict, int firstIndex, int secondIndex)
    {
        if (firstIndex < 0 || secondIndex < 0 || firstIndex >= orderedDict.Count || secondIndex >= orderedDict.Count || firstIndex == secondIndex)
        {
            return false;
        }

        T item1Key = orderedDict.Keys[firstIndex];
        TS item1Value = orderedDict.ByIndex[firstIndex];
        T item2Key = orderedDict.Keys[secondIndex];
        TS item2Value = orderedDict.ByIndex[secondIndex];

        orderedDict.RemoveAt(firstIndex);
        orderedDict.RemoveAt(firstIndex < secondIndex ? secondIndex - 1 : secondIndex);
        if (secondIndex < firstIndex)
        {
            firstIndex--;
        }
        orderedDict.Insert(firstIndex, item2Key, item2Value);
        orderedDict.Insert(secondIndex, item1Key, item1Value);
        return true;
    }

    public static bool Move<T, TS>(this OrderedDictionary<T, TS> orderedDict, int firstIndex, int secondIndex)
    {
        if (firstIndex < 0 || secondIndex < 0 || firstIndex >= orderedDict.Count || secondIndex >= orderedDict.Count || firstIndex == secondIndex)
        {
            return false;
        }

        T itemKey = orderedDict.Keys[firstIndex];
        TS itemValue = orderedDict.ByIndex[firstIndex];

        orderedDict.RemoveAt(firstIndex);
        orderedDict.Insert(secondIndex, itemKey, itemValue);
        return true;
    }

    public static void Move<T>(this IList<T> list, int firstIndex, int secondIndex)
    {
        if (firstIndex < 0 || secondIndex < 0 || firstIndex >= list.Count || secondIndex >= list.Count || firstIndex == secondIndex)
        {
            return;
        }

        T item = list[firstIndex];
        list.RemoveAt(firstIndex);
        list.Insert(secondIndex, item);
    }

    [GeneratedRegex(@"#x(?<offset>\d{2})")]
    private static partial Regex ManualXRegex();
    [GeneratedRegex(@"#y(?<offset>\d{2})")]
    private static partial Regex ManualYRegex();
}

public class SKColorJsonConverter : JsonConverter<SKColor>
{
    public override SKColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string html = reader.GetString();
        return new(
            byte.Parse(html[2..4], NumberStyles.HexNumber),
            byte.Parse(html[4..6], NumberStyles.HexNumber),
            byte.Parse(html[6..8], NumberStyles.HexNumber),
            byte.Parse(html[..2], NumberStyles.HexNumber)
        );
    }

    public override void Write(Utf8JsonWriter writer, SKColor value, JsonSerializerOptions options) =>
        writer.WriteStringValue($"{value.Alpha:X2}{value.Red:X2}{value.Green:X2}{value.Blue:X2}");
}
