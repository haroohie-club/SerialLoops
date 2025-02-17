using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Codeuctivity.ImageSharpCompare;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using Moq;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors;
using SerialLoops.Views.Editors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace SerialLoops.Tests.Headless.Editors;

[TestFixture]
public class BackgroundEditorTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;
    private MainWindowViewModel _mainWindowViewModel;
    private Project _project;

    private BgTableFile _bgTableFile;
    private ExtraFile _extra;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _log = new();
        if (Path.Exists("ui_vals.json"))
        {
            _uiVals = JsonSerializer.Deserialize<UiVals>(File.ReadAllText("ui_vals.json"));
        }
        else
        {
            _uiVals = new()
            {
                AssetsDirectory = Environment.GetEnvironmentVariable("ASSETS_DIRECTORY")!,
            };
        }

        _extra = new();
        _extra.Initialize(File.ReadAllBytes(Path.Combine(_uiVals.AssetsDirectory, "EXTRA.bin")), 0, _log);

        _bgTableFile = new();
        _bgTableFile.Initialize(File.ReadAllBytes(Path.Combine(_uiVals.AssetsDirectory, "BGTBL.bin")), 1, _log);
    }

    [SetUp]
    public void Setup()
    {
        Mock<Project> projectMock = new();
        projectMock.SetupCommonMocks();
        _project = projectMock.Object;
        _project.SetupMockedProperties(_uiVals);
        _project.Extra = _extra;
        Mock<ArchiveFile<GraphicsFile>> grpMock = new();
        grpMock.SetupGrpMock(_uiVals.AssetsDirectory, _log);
        _project.Grp = grpMock.Object;

        Mock<MainWindowViewModel> windowMock = new();
        windowMock.SetupCommonMocks();
        _mainWindowViewModel = windowMock.Object;
        _mainWindowViewModel.SetupMockedProperties(_project, _log);
    }

    [AvaloniaTest, Parallelizable(ParallelScope.All)]
    [TestCase("KBG00", 0xB6, "KINETIC_SCREEN")]
    [TestCase("BG_ROUD2", 0x4C, "TEX_BG")]
    [TestCase("BG_OKUD0", 0x2F, "TEX_BG")]
    [TestCase("EV352", 0x11B, "TEX_CG")]
    [TestCase("EV150_D", 0xE2, "TEX_CG_DUAL_SCREEN")]
    [TestCase("EV285", 0x106, "TEX_CG_WIDE")]
    [TestCase("EV381", 0x125, "TEX_CG_SINGLE")]
    public void BackgroundEditor_ReplacementIsApproximatelyIdempotent(string name, int bgTableIndex, string type)
    {
        BackgroundItem bgItem = new(name, bgTableIndex, _bgTableFile.BgTableEntries[bgTableIndex], _project);
        BackgroundEditorViewModel editorVm = new(bgItem, _mainWindowViewModel, _project, _log);

        using MemoryStream initialBg = new();
        editorVm.BgBitmap.Encode(initialBg, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        string filePath = $"{name}.png";
        File.WriteAllBytes(filePath, initialBg.ToArray());
        initialBg.Seek(0, SeekOrigin.Begin);

        Assert.That(bgItem.SetBackground(SKBitmap.Decode(File.ReadAllBytes(filePath)),
            new TestProgressTracker(), _log, _project.Localize), Is.True);
        using MemoryStream finalBg = new();
        editorVm.BgBitmap.Encode(finalBg, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        finalBg.Seek(0, SeekOrigin.Begin);

        Image<Rgb24> maskImage = new(new(), editorVm.BgBitmap.Width, editorVm.BgBitmap.Height);
        ICompareResult diff = ImageSharpCompare.CalcDiff(finalBg, initialBg, maskImage);
        maskImage.Save($"{name}_diff.png", new PngEncoder());
        TestContext.AddTestAttachment($"{name}_diff.png", "Diff mask of original and replaced images");
        _log.Log($"Mean error: {diff.MeanError}");
        Assert.That(diff.MeanError, Is.LessThan(0.5));

        File.Delete(filePath);
    }

    [AvaloniaTest, Parallelizable(ParallelScope.Self)]
    [TestCase("colorful.jpg", "BG_ROUD2", 0x4C, 10, "TEX_BG")]
    [TestCase("too-colorful.jpg", "BG_ROUD2", 0x4C, 25, "TEX_BG")]
    [TestCase("colorful.jpg", "EV352", 0x4C, 10, "TEX_CG")]
    [TestCase("too-colorful.jpg", "EV352", 0x4C, 25, "TEX_CG")]
    [TestCase("pattern.png", "KBG00", 0xB6, 10, "KINETIC_SCREEN")]
    public void BackgroundEditor_ReplacementIsAccurate(string replacementImage, string name, int bgTableIndex, int threshold, string type)
    {
        BackgroundItem bgItem = new(name, bgTableIndex, _bgTableFile.BgTableEntries[bgTableIndex], _project);
        BackgroundEditorViewModel editorVm = new(bgItem, _mainWindowViewModel, _project, _log);

        using FileStream fs = File.OpenRead(Path.Combine(_uiVals.AssetsDirectory, replacementImage));
        using MemoryStream initialBg = new(File.ReadAllBytes(Path.Combine(_uiVals.AssetsDirectory, replacementImage)));
        bgItem.SetBackground(SKBitmap.Decode(fs), new ProgressDialogViewModel("Test"), _log, _project.Localize);

        using MemoryStream finalBg = new();
        editorVm.BgBitmap.Encode(finalBg, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        finalBg.Seek(0, SeekOrigin.Begin);

        Image<Rgb24> maskImage = new(new(), editorVm.BgBitmap.Width, editorVm.BgBitmap.Height);
        ICompareResult diff = ImageSharpCompare.CalcDiff(finalBg, initialBg, maskImage);
        maskImage.Save($"{name}_{replacementImage}_diff.png", new PngEncoder());
        TestContext.AddTestAttachment($"{name}_{replacementImage}_diff.png", "Diff mask of original and replaced images");
        _log.Log($"Mean error: {diff.MeanError}");
        Assert.That(diff.MeanError, Is.LessThan(threshold));
    }

    [AvaloniaTest]
    public void BackgroundEditor_KbgReplacementFailsOnComplexImageButDoesntThrow()
    {
        BackgroundItem bgItem = new("KBG00", 0xB6, _bgTableFile.BgTableEntries[0xB6], _project);
        BackgroundEditorViewModel editorVm = new(bgItem, _mainWindowViewModel, _project, _log);

        using FileStream fs = File.OpenRead(Path.Combine(_uiVals.AssetsDirectory, "too-colorful.jpg"));
        using MemoryStream initialBg = new();
        editorVm.BgBitmap.Encode(initialBg, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        initialBg.Seek(0, SeekOrigin.Begin);
        Assert.DoesNotThrow(() => bgItem.SetBackground(SKBitmap.Decode(fs), new ProgressDialogViewModel("Test"), _log, _project.Localize));

        using MemoryStream finalBg = new();
        editorVm.BgBitmap.Encode(finalBg, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        finalBg.Seek(0, SeekOrigin.Begin);

        Image<Rgb24> maskImage = new(new(), editorVm.BgBitmap.Width, editorVm.BgBitmap.Height);
        ICompareResult diff = ImageSharpCompare.CalcDiff(finalBg, initialBg, maskImage);
        maskImage.Save("KBG00_failed_replacement_diff.png", new PngEncoder());
        TestContext.AddTestAttachment("KBG00_failed_replacement_diff.png", "Diff mask of original image and itself (as replacement should have failed)");
        _log.Log($"Mean error: {diff.MeanError}");
        Assert.That(diff.MeanError, Is.Zero);
    }

    [AvaloniaTest]
    [TestCase("EV150_D", 0xE2, "TEX_CG_DUAL_SCREEN")]
    [TestCase("EV285", 0x106, "TEX_CG_WIDE")]
    [TestCase("EV381", 0x125, "TEX_CG_SINGLE")]
    public void BackgroundEditor_CgNameCanBeChanged(string name, int bgTableIndex, string type)
    {
        BackgroundItem bgItem = new(name, bgTableIndex, _bgTableFile.BgTableEntries[bgTableIndex], _project);
        BackgroundEditorViewModel editorVm = new(bgItem, _mainWindowViewModel, _project, _log);

        BackgroundEditorView editor = new() { DataContext = editorVm };
        string newCgName = "Test CG";

        Window window = new() { Content = editor };
        window.Show();

        Assert.That(editor.CgBox.Focus(), Is.True);
        editor.CgBox.SelectAll();
        window.KeyTextInput(newCgName);
        int currentFrame = 0;
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        Assert.Multiple(() =>
        {
            Assert.That(editor.CgBox.Text, Is.EqualTo(newCgName));
            Assert.That(_extra.Cgs.First(c => c.BgId == bgTableIndex).Name, Is.EqualTo(newCgName.GetOriginalString(_project)));
        });
    }

    [AvaloniaTest]
    [TestCase("KBG00", 0xB6, "KINETIC_SCREEN")]
    [TestCase("BG_ROUD2", 0x4C, "TEX_BG")]
    [TestCase("BG_OKUD0", 0x2F, "TEX_BG")]
    public void BackgroundEditor_CgBoxNotVisibleOnNonCg(string name, int bgTableIndex, string type)
    {
        BackgroundItem bgItem = new(name, bgTableIndex, _bgTableFile.BgTableEntries[bgTableIndex], _project);
        BackgroundEditorViewModel editorVm = new(bgItem, _mainWindowViewModel, _project, _log);

        BackgroundEditorView editor = new() { DataContext = editorVm };
        Window window = new() { Content = editor };
        window.Show();
        int currentFrame = 0;
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        Assert.That(editor.CgPanel.IsVisible, Is.False);
    }
}
