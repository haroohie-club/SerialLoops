using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using SerialLoops.ViewModels;
using SerialLoops.Views;

namespace SerialLoops.Tests.Headless;

public static class Helpers
{
    public static void CaptureAndSaveFrame(this Window window, string artifactsDir, string nameOfTest, ref int currentFrame)
    {
        if (!Directory.Exists(Path.Combine(artifactsDir, nameOfTest)))
        {
            Directory.CreateDirectory(Path.Combine(artifactsDir, nameOfTest));
        }
        string file = Path.Combine(artifactsDir, nameOfTest, $"{currentFrame++:D2}.png");
        window.CaptureRenderedFrame()?.Save(file);
        TestContext.AddTestAttachment(file, $"{currentFrame}");
    }
}
