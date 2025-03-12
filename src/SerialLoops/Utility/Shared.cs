using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace SerialLoops.Utility;

public static class Shared
{
    public static CancellationTokenSource AudioReplacementCancellation { get; set; } = new();

    public static string[] SupportedImageFiletypes => ["*.bmp", "*.gif", "*.heif", "*.jpg", "*.jpeg", "*.png", "*.webp",];
    public static string[] SupportedAudioFiletypes => ["*.wav", "*.flac", "*.mp3", "*.ogg",];

    public static void SaveGif(this IEnumerable<SKBitmap> frames, string fileName, IProgressTracker tracker)
    {
        using Image<Rgba32> gif = new(frames.Max(f => f.Width), frames.Max(f => f.Height));
        gif.Metadata.GetGifMetadata().RepeatCount = 0;

        tracker.Focus(Strings.Converting_frames___, 1);
        IEnumerable<Image<Rgba32>> gifFrames = frames.Select(f => Image.LoadPixelData<Rgba32>(f.Pixels.Select(c => new Rgba32(c.Red, c.Green, c.Blue, c.Alpha)).ToArray(), f.Width, f.Height));
        tracker.Finished++;
        tracker.Focus(Strings.Adding_frames_to_GIF___, gifFrames.Count());
        foreach (Image<Rgba32> gifFrame in gifFrames)
        {
            GifFrameMetadata metadata = gifFrame.Frames.RootFrame.Metadata.GetGifMetadata();
            metadata.FrameDelay = 2;
            metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            gif.Frames.AddFrame(gifFrame.Frames.RootFrame);
            tracker.Finished++;
        }
        gif.Frames.RemoveFrame(0);

        tracker.Focus(Strings.Saving_GIF___, 1);
        gif.SaveAsGif(fileName);
        tracker.Finished++;
    }

    public static SKBitmap GetCharacterVoicePortrait(Project project, ILogger log, CharacterItem character)
    {
        ItemDescription id = project.Items.Find(i => i.Name.Equals("SYSTEX_SYS_CMN_B46"));
        if (id is not SystemTextureItem tex)
        {
            log.LogError(string.Format(Strings.Failed_to_load_character_progress_voice_for__0__, character.DisplayName));
            return null;
        }
        SKBitmap bitmap = tex.GetTexture();

        // Crop a 16x16 bitmap portrait
        SKBitmap portrait = new(16, 16);
        int portraitIndex = (int)character.MessageInfo.Character;
        int charNum = Math.Min(portraitIndex, 10) - 1;
        int x = (charNum % 4) * 32;
        int z = (charNum / 4) * 32;

        SKRectI cropRect = new(x + 8, z + 4, x + 24, z + 20);
        bitmap.ExtractSubset(portrait, cropRect);
        return portrait;
    }
}
