using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SerialLoops.Assets;
using SerialLoops.Lib.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace SerialLoops.Utility
{
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
    }
}
