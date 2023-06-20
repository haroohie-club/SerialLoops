using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

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
                project.Save();
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
                if (explorer.Viewer.SelectedItem.Text.Equals(oldName))
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
                project.Save();
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
    }
}
