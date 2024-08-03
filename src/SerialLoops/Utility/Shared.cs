using System.Threading;

namespace SerialLoops.Utility
{
    public static class Shared
    {
        public static CancellationTokenSource AudioReplacementCancellation { get; set; } = new();

        public static string[] SupportedImageFiletypes => ["*.bmp", "*.gif", "*.heif", "*.jpg", "*.jpeg", "*.png", "*.webp",];
        public static string[] SupportedAudioFiletypes => ["*.wav", "*.flac", "*.mp3", "*.ogg",];
    }
}
