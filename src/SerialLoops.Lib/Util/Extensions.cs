using HaruhiChokuretsuLib.Archive.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;
using SkiaSharp;
using VCDiff.Shared;

namespace SerialLoops.Lib.Util
{
    public static class Extensions
    {
        public static string GetSubstitutedString(this string line, Project project)
        {
            if (project.LangCode != "ja")
            {
                // we replace " in the base library, but we don't want to do that here since we'll rely on rich-text editing instead
                return string.Join("", line.Select(c => c != '“' ? (project.FontReplacement.ReverseLookup(c)?.ReplacedCharacter ?? c) : c));
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
                string originalString = string.Join("", line.Select(c => project.FontReplacement.ContainsKey(c) ? project.FontReplacement[c].OriginalCharacter : c));
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

        public static void CollectGarbage(this EventFile evt)
        {
            IEnumerable<string> conditionalContainingCommands = new CommandVerb[] { CommandVerb.VGOTO, CommandVerb.SCENE_GOTO, CommandVerb.SCENE_GOTO2 }.Select(c => c.ToString());
            List<UsedIndex> usedIndices = new();
            foreach (ScriptCommandInvocation conditionalCommand in evt.ScriptSections.SelectMany(s => s.Objects).Where(c => conditionalContainingCommands.Contains(c.Command.Mnemonic)))
            {
                usedIndices.Add(new() { Command = conditionalCommand, Index = conditionalCommand.Parameters[0] });
            }
            if (usedIndices.DistinctBy(c => c.Index).Count() < evt.ConditionalsSection.Objects.Count)
            {
                for (short i = 0; i < evt.ConditionalsSection.Objects.Count; i++)
                {
                    if (!usedIndices.Select(idx => idx.Index).Contains(i))
                    {
                        evt.ConditionalsSection.Objects.RemoveAt(i);
                        for (int j = 0; j < usedIndices.Count; j++)
                        {
                            if (usedIndices[j].Index >= i)
                            {
                                usedIndices[j].Command.Parameters[0]--;
                                usedIndices[j].Index--;
                            }
                        }
                        i--;
                    }
                }
            }
        }

        private class UsedIndex
        {
            public ScriptCommandInvocation Command { get; set; }
            public short Index { get; set; }
        }
    }

    public class SKColorJsonConverter : JsonConverter<SKColor>
    {
        public override SKColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string html = reader.GetString();
            return new(
                byte.Parse(html[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(html[4..6], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(html[6..8], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(html[0..2], System.Globalization.NumberStyles.HexNumber)
                );
        }

        public override void Write(Utf8JsonWriter writer, SKColor value, JsonSerializerOptions options) => writer.WriteStringValue($"{value.Alpha:X2}{value.Red:X2}{value.Green:X2}{value.Blue:X2}");
    }
}
