using Eto.Forms;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SerialLoops.Utility
{
    public static class Shared
    {
        public static void RenameItem(Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log, bool overrideRename = false)
        {
            ItemDescription item = project.FindItem(explorer.Viewer.SelectedItem?.Text);
            if (item is not null)
            {
                if (!item.CanRename && !overrideRename)
                {
                    MessageBox.Show("Can't rename this item directly -- open it to rename it!", "Can't Rename Item", MessageBoxType.Warning);
                    return;
                }
                DocumentPage openTab = tabs.Tabs.Pages.FirstOrDefault(p => p.Text == item.DisplayNameWithStatus);
                ItemRenameDialog renameDialog = new(item, project, log);
                renameDialog.ShowModal();
                explorer.Viewer.SelectedItem.Text = item.DisplayName;
                if (openTab is not null)
                {
                    openTab.Text = item.DisplayNameWithStatus;
                }
                ((TreeGridView)explorer.Viewer.Control).ReloadData();
                project.ItemNames[item.Name] = item.DisplayName;
                project.Save(log);
            }
        }
        public static void RenameItem(Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log, string newName)
        {
            ItemDescription item = project.FindItem(explorer.Viewer.SelectedItem?.Text);
            RenameItem(item, project, explorer, tabs, log, newName);
        }
        public static void RenameItem(ItemDescription item, Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log, string newName)
        {
            if (item is not null)
            {
                string oldName = item.DisplayName;
                DocumentPage openTab = tabs.Tabs.Pages.FirstOrDefault(p => p.Text == item.DisplayNameWithStatus);
                item.Rename(newName);
                if (explorer.Viewer.SelectedItem?.Text.Equals(oldName) ?? false)
                {
                    // Unfortunately, there doesn't seem to be a good way to ensure the item gets rename if we've selected a different item
                    explorer.Viewer.SelectedItem.Text = item.DisplayName;
                }
                if (openTab is not null)
                {
                    openTab.Text = item.DisplayNameWithStatus;
                }
                ((TreeGridView)explorer.Viewer.Control).ReloadData();
                project.ItemNames[item.Name] = item.DisplayName;
                project.Save(log);
            }
        }

        public static void SaveGif(this IEnumerable<SKBitmap> frames, string fileName, IProgressTracker tracker)
        {
            using Image<Rgba32> gif = new(frames.Max(f => f.Width), frames.Max(f => f.Height));
            gif.Metadata.GetGifMetadata().RepeatCount = 0;

            tracker.Focus("Converting frames...", 1);
            IEnumerable<Image<Rgba32>> gifFrames = frames.Select(f => Image.LoadPixelData<Rgba32>(f.Pixels.Select(c => new Rgba32(c.Red, c.Green, c.Blue, c.Alpha)).ToArray(), f.Width, f.Height));
            tracker.Finished++;
            tracker.Focus("Adding frames to GIF...", gifFrames.Count());
            foreach (Image<Rgba32> gifFrame in gifFrames)
            {
                GifFrameMetadata metadata = gifFrame.Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = 2;
                metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
                gif.Frames.AddFrame(gifFrame.Frames.RootFrame);
                tracker.Finished++;
            }
            gif.Frames.RemoveFrame(0);

            tracker.Focus("Saving GIF...", 1);
            gif.SaveAsGif(fileName);
            tracker.Finished++;
        }

        public static void DrawText(string text, SKCanvas canvas, SKPaint color, Project project, int x = 10, int y = 352, bool formatting = true)
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
    }
}
