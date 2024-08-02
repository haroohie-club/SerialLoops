using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Font;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;
using static SerialLoops.Lib.Script.SpritePositioning;

namespace SerialLoops.Lib.Util
{
    public static class Extensions
    {
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

        public static void DrawHaroohieText(this SKCanvas canvas, string text, SKPaint color, Project project, int x = 10, int y = 352, bool formatting = true)
        {
            int currentX = x;
            int currentY = y;

            for (int i = 0; i < text.Length; i++)
            {
                // handle newlines
                if (text[i] == '\n')
                {
                    currentX = 10;
                    currentY += 14;
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
                        color = int.Parse(Regex.Match(text[i..(i + 4)], @"#P(?<id>\d{2})").Groups["id"].Value) switch
                        {
                            1 => DialogueScriptParameter.Paint01,
                            2 => DialogueScriptParameter.Paint02,
                            3 => DialogueScriptParameter.Paint03,
                            4 => DialogueScriptParameter.Paint04,
                            5 => DialogueScriptParameter.Paint05,
                            6 => DialogueScriptParameter.Paint06,
                            7 => DialogueScriptParameter.Paint07,
                            _ => DialogueScriptParameter.Paint00,
                        };
                        i += 3;
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
                        canvas.DrawBitmap(project.FontBitmap, new SKRect(0, charIndex * 16, 16, (charIndex + 1) * 16),
                            new SKRect(currentX, currentY, currentX + 16, currentY + 16), color);
                    }
                }
                FontReplacement replacement = project.FontReplacement.ReverseLookup(text[i]);
                if (replacement is not null && project.LangCode != "ja")
                {
                    currentX += replacement.Offset;
                }
                else
                {
                    currentX += 14;
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
                byte.Parse(html[0..2], NumberStyles.HexNumber)
                );
        }

        public override void Write(Utf8JsonWriter writer, SKColor value, JsonSerializerOptions options) => writer.WriteStringValue($"{value.Alpha:X2}{value.Red:X2}{value.Green:X2}{value.Blue:X2}");
    }
}
