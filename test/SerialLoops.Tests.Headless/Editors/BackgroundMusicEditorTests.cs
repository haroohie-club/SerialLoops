using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Codeuctivity.ImageSharpCompare;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using Moq;
using NAudio.Wave;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Lib.Util.WaveformRenderer;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Editors;
using SerialLoops.Views.Editors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using Image = SixLabors.ImageSharp.Image;

namespace SerialLoops.Tests.Headless.Editors;

[TestFixture]
public class BackgroundMusicEditorTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;
    private MainWindowViewModel _mainWindowViewModel;
    private Project _project;
    private ExtraFile _extra;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _log = new();
        if (Path.Exists("ui_vals.json"))
        {
            _uiVals = JsonSerializer.Deserialize<UiVals>(await File.ReadAllTextAsync("ui_vals.json"));
        }
        else
        {
            _uiVals = await UiVals.DownloadTestAssets();
        }

        _extra = new();
        _extra.Initialize(await File.ReadAllBytesAsync(Path.Combine(_uiVals.AssetsDirectory, "EXTRA.bin")), 0, _log);
    }

    [SetUp]
    public void Setup()
    {
        Mock<Project> projectMock = new();
        projectMock.SetupCommonMocks();
        _project = projectMock.Object;
        _project.SetupMockedProperties(_uiVals);
        _project.Extra = _extra;

        Mock<MainWindowViewModel> windowMock = new() { CallBase = true };
        windowMock.SetupCommonMocks();
        _mainWindowViewModel = windowMock.Object;
        _mainWindowViewModel.SetupMockedProperties(_project, _log);

        string testId = Guid.NewGuid().ToString();
        ConfigUser configUser = new() { UserDirectory = Path.Combine(Path.GetTempPath(), testId) };
        ConfigFactoryMock configFactory = new($"{testId}.json", configUser);
        _mainWindowViewModel.CurrentConfig = configFactory.LoadConfig(s => s, _log);
        _project.ConfigUser = _mainWindowViewModel.CurrentConfig;
        _project.Name = nameof(BackgroundMusicEditorTests);

        IO.CopyFileToDirectories(_project, Path.Combine(_uiVals.AssetsDirectory, "BGM027.bin"), Path.Combine("original", "bgm", "BGM027.bin"), _log);
        IO.CopyFileToDirectories(_project, Path.Combine(_uiVals.AssetsDirectory, "BGM030.bin"), Path.Combine("original", "bgm", "BGM030.bin"), _log);
        IO.CopyFileToDirectories(_project, Path.Combine(_uiVals.AssetsDirectory, "BGM031.bin"), Path.Combine("original", "bgm", "BGM031.bin"), _log);
        IO.CopyFileToDirectories(_project, Path.Combine(_uiVals.AssetsDirectory, "BGM027.bin"), Path.Combine("rom", "data", "bgm", "BGM027.bin"), _log);
        IO.CopyFileToDirectories(_project, Path.Combine(_uiVals.AssetsDirectory, "BGM030.bin"), Path.Combine("rom", "data", "bgm", "BGM030.bin"), _log);
        IO.CopyFileToDirectories(_project, Path.Combine(_uiVals.AssetsDirectory, "BGM031.bin"), Path.Combine("rom", "data", "bgm", "BGM031.bin"), _log);

        _project.Items =
        [
            new BackgroundMusicItem(Path.Combine(_project.IterativeDirectory, "rom", "data", "bgm", "BGM027.bin"), 19, _project),
            new BackgroundMusicItem(Path.Combine(_project.IterativeDirectory, "rom", "data", "bgm", "BGM030.bin"), 22, _project),
            new BackgroundMusicItem(Path.Combine(_project.IterativeDirectory, "rom", "data", "bgm", "BGM031.bin"), 23, _project),
        ];
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Directory.Delete(_project.MainDirectory, true);
    }

    [AvaloniaTest]
    [TestCase("BGM027", "Looping")]
    [TestCase("BGM030", "Non-Looping")]
    [TestCase("BGM031", "No Title")]
    public void BackgroundMusicEditor_ReplacementIsIdempotent(string bgmName, string loopingType)
    {
        BackgroundMusicItem bgm = (BackgroundMusicItem)_project.Items.First(i => i.Name == bgmName);
        Assert.That(bgm, Is.Not.Null);

        using MemoryStream originalStream = new();
        string wavFile = $"{bgmName}.wav";
        WaveFileWriter.CreateWaveFile(wavFile, bgm.GetWaveProvider(_log, false));
        Assert.That(File.Exists(wavFile), Is.True);
        using WaveFileReader originalWaveStream = new(wavFile);
        WaveformRenderer.Render(originalWaveStream, WaveFormRendererSettings.StandardSettings).Encode(originalStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        originalStream.Seek(0, SeekOrigin.Begin);
        Image originalWaveform = Image.Load(originalStream);
        originalWaveStream.Close();

        string replacedFile = $"{bgmName}_replaced.wav";
        bgm.Replace(wavFile, _project, replacedFile, false, 0, 0, _log, new TestProgressTracker(), new());
        using MemoryStream replacedStream = new();
        using WaveFileReader replacedWaveStream = new(replacedFile);
        Assert.That(File.Exists(replacedFile), Is.True);
        WaveformRenderer.Render(replacedWaveStream, WaveFormRendererSettings.StandardSettings).Encode(replacedStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        replacedStream.Seek(0, SeekOrigin.Begin);
        Image replacedWaveform = Image.Load(replacedStream);

        Image<Rgb24> maskImage = new(new(), originalWaveform.Width, originalWaveform.Height);
        ICompareResult diff = ImageSharpCompare.CalcDiff(replacedWaveform, originalWaveform, maskImage);
        maskImage.Save($"{bgmName}_diff.png", new PngEncoder());
        TestContext.AddTestAttachment($"{bgmName}_diff.png", "Diff mask of original and replaced waveforms");
        _log.Log($"Mean error: {diff.MeanError}");
        Assert.That(diff.MeanError, Is.Zero);
    }

    [AvaloniaTest]
    [TestCase("BGM027", "Looping")]
    [TestCase("BGM030", "Non-Looping")]
    [TestCase("BGM031", "No Title")]
    public void BackgroundMusicEditor_ReplacementAndRestoreWork(string bgmName, string loopingType)
    {
        BackgroundMusicItem bgm = (BackgroundMusicItem)_project.Items.First(i => i.Name == bgmName);
        Assert.That(bgm, Is.Not.Null);

        BackgroundMusicEditorViewModel editorVm = new(bgm, _mainWindowViewModel, _project, _log, initializePlayer: false);

        using MemoryStream originalStream = new();
        string wavFile = $"{bgmName}.wav";
        WaveFileWriter.CreateWaveFile(wavFile, bgm.GetWaveProvider(_log, false));
        Assert.That(File.Exists(wavFile), Is.True);
        using WaveFileReader originalWaveStream = new(wavFile);
        WaveformRenderer.Render(originalWaveStream, WaveFormRendererSettings.StandardSettings).Encode(originalStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        originalStream.Seek(0, SeekOrigin.Begin);
        Image originalWaveform = Image.Load(originalStream);
        originalWaveStream.Close();

        string replacedFile = $"{bgmName}_replaced.wav";
        bgm.Replace(Path.Combine(_uiVals.AssetsDirectory, "music.wav"), _project, replacedFile, false, 0, 0, _log, new TestProgressTracker(), new());
        using MemoryStream replacedStream = new();
        using WaveFileReader replacedWaveStream = new(replacedFile);
        Assert.That(File.Exists(replacedFile), Is.True);
        WaveformRenderer.Render(replacedWaveStream, WaveFormRendererSettings.StandardSettings).Encode(replacedStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        replacedStream.Seek(0, SeekOrigin.Begin);
        Image replacedWaveform = Image.Load(replacedStream);
        replacedWaveStream.Close();

        Image<Rgb24> replacedMaskImage = new(new(), originalWaveform.Width, originalWaveform.Height);
        ICompareResult replacedDiff = ImageSharpCompare.CalcDiff(replacedWaveform, originalWaveform, replacedMaskImage);
        replacedMaskImage.Save($"{bgmName}_diff.png", new PngEncoder());
        TestContext.AddTestAttachment($"{bgmName}_diff.png", "Diff mask of original and replaced waveforms");
        _log.Log($"Mean error: {replacedDiff.MeanError}");
        Assert.That(replacedDiff.MeanError, Is.Not.Zero);

        editorVm.RestoreCommand.Execute(null);
        string restoredFile = $"{bgmName}_restored.wav";
        WaveFileWriter.CreateWaveFile(restoredFile, editorVm.Bgm.GetWaveProvider(_log, false));
        using MemoryStream restoredStream = new();
        using WaveFileReader restoredWaveStream = new(restoredFile);
        Assert.That(File.Exists(restoredFile), Is.True);
        WaveformRenderer.Render(restoredWaveStream, WaveFormRendererSettings.StandardSettings).Encode(restoredStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        restoredStream.Seek(0, SeekOrigin.Begin);
        Image restoredWaveform = Image.Load(restoredStream);

        Image<Rgb24> restoredMaskImage = new(new(), originalWaveform.Width, originalWaveform.Height);
        ICompareResult restoredDiff = ImageSharpCompare.CalcDiff(restoredWaveform, originalWaveform, restoredMaskImage);
        restoredMaskImage.Save($"{bgmName}_restore_diff.png", new PngEncoder());
        TestContext.AddTestAttachment($"{bgmName}_restore_diff.png", "Diff mask of original and restored waveforms");
        _log.Log($"Mean error: {restoredDiff.MeanError}");
        Assert.That(restoredDiff.MeanError, Is.Zero);
    }

    [AvaloniaTest]
    [TestCase("BGM027")]
    [TestCase("BGM030")]
    public void BackgroundMusicEditor_CanChangeTitle(string bgmName)
    {
        BackgroundMusicItem bgm = (BackgroundMusicItem)_project.Items.First(i => i.Name == bgmName);
        Assert.That(bgm, Is.Not.Null);

        BackgroundMusicEditorViewModel editorVm = new(bgm, _mainWindowViewModel, _project, _log, initializePlayer: false);
        BackgroundMusicEditorView editor = new() { DataContext = editorVm };
        Window window = new() { Content = editor };
        window.Show();

        int currentFrame = 0;
        TextBox titleBox = editor.Player.Get<TextBox>("TrackNameBox");
        Assert.That(titleBox, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(titleBox.IsVisible, Is.True);
            Assert.That(titleBox.Text, Is.EqualTo(_extra.Bgms.First(b => b.Index == bgm.Index).Name.GetSubstitutedString(_project)));
        });

        const string newTitle = "BGM Title";
        titleBox.Focus();
        titleBox.SelectAll();
        window.KeyTextInput(newTitle);
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        Assert.Multiple(() =>
        {
            Assert.That(_extra.Bgms.First(b => b.Index == bgm.Index).Name.GetSubstitutedString(_project), Is.EqualTo(newTitle));
            Assert.That(bgm.DisplayName, Does.EndWith(newTitle));
        });
    }

    [AvaloniaTest]
    public void BackgroundMusicEditor_TitleBoxNotVisibleWhenTrackHasNoTitle()
    {
        BackgroundMusicItem bgm = (BackgroundMusicItem)_project.Items.First(i => i.Name == "BGM031");
        Assert.That(bgm, Is.Not.Null);

        BackgroundMusicEditorViewModel editorVm = new(bgm, _mainWindowViewModel, _project, _log, initializePlayer: false);
        BackgroundMusicEditorView editor = new() { DataContext = editorVm };
        Window window = new() { Content = editor };
        window.Show();

        int currentFrame = 0;
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        TextBox titleBox = editor.Player.Get<TextBox>("TrackNameBox");
        Assert.That(titleBox, Is.Not.Null);
        Assert.That(titleBox.IsVisible, Is.False);
    }

    [AvaloniaTest]
    public void BackgroundMusicEditor_CanOpenAfterProjectRename()
    {
        BackgroundMusicItem bgm = (BackgroundMusicItem)_project.Items.First(i => i.Name == "BGM031");
        Assert.That(bgm, Is.Not.Null);

        string testId = Guid.NewGuid().ToString();
        ConfigUser configUser = new() { UserDirectory = Path.Combine(Path.GetTempPath(), testId) };

        ConfigFactoryMock configFactory = new($"{testId}.json", configUser);
        Mock<MainWindowViewModel> windowMock = new();
        MainWindowViewModel mainWindowVm = windowMock.Object;
        mainWindowVm.Window = new() { DataContext = mainWindowVm, ConfigurationFactory = configFactory };
        mainWindowVm.Window.Show();
        Directory.CreateDirectory(mainWindowVm.CurrentConfig.ProjectsDirectory);

        Mock<Project> projectMock = new();
        projectMock.SetupCommonMocks();
        Project project = projectMock.Object;
        project.SetupMockedProperties(_uiVals);
        project.Extra = _extra;
        project.ConfigUser = mainWindowVm.CurrentConfig;
        mainWindowVm.OpenProject = project;
        Directory.CreateDirectory(Path.Combine(mainWindowVm.CurrentConfig.ProjectsDirectory, project.Name));
        project.Save(_log);

        mainWindowVm.RenameProjectCommand.Execute(null);
        mainWindowVm.Window.KeyTextInput("RenameTest");
        mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");

        Assert.That(project.Name, Is.EqualTo("RenameTest"));
        BackgroundMusicEditorViewModel editorVm = new(bgm, mainWindowVm, _project, _log, initializePlayer: false);
        BackgroundMusicEditorView editor = new() { DataContext = editorVm };
        Window window = new() { Content = editor };
        Assert.DoesNotThrow(window.Show);

        mainWindowVm.RenameProjectCommand.Execute(null);
        mainWindowVm.Window.KeyTextInput(nameof(BackgroundEditorTests));
        mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
    }
}
