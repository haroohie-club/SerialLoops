using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;

namespace SerialLoops.Tests.Headless
{
    public static class Helpers
    {
        public static void CaptureAndSaveFrame(this Window window, string artifactsDir, string nameOfTest, ref int currentFrame)
        {
            if (!Directory.Exists(Path.Combine(artifactsDir, nameOfTest)))
            {
                Directory.CreateDirectory(Path.Combine(artifactsDir, nameOfTest));
            }
            window.CaptureRenderedFrame()?.Save(Path.Combine(artifactsDir, nameOfTest, $"{currentFrame++:D2}.png"));
        }
    }
}
