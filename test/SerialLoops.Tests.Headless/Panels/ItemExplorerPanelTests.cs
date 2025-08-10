using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using Moq;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.Views;
using SerialLoops.Views.Panels;

namespace SerialLoops.Tests.Headless.Panels;

public class ItemExplorerPanelTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;
    private Project _project;
    private ConfigUser _configUser;

    private BgTableFile _bgTableFile;
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

        _bgTableFile = new();
        _bgTableFile.Initialize(await File.ReadAllBytesAsync(Path.Combine(_uiVals.AssetsDirectory, "BGTBL.bin")), 1, _log);
    }

    [SetUp]
    public void Setup()
    {
        Mock<Project> projectMock = new();
        projectMock.SetupCommonMocks();
        ConfigUser configUser = new() { UserDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), };
        ConfigFactoryMock factory = new("config.json", configUser);
        _configUser = factory.LoadConfig(l => l, _log);
        _project = projectMock.Object;
        _project.ConfigUser = _configUser;
        _project.Name = "Test";
        Directory.CreateDirectory(Path.Combine(_project.MainDirectory));
        _project.SetupMockedProperties(_uiVals);
        _project.Extra = _extra;
        Mock<ArchiveFile<GraphicsFile>> grpMock = new();
        grpMock.SetupGrpMock(_uiVals.AssetsDirectory, _log);
        _project.Grp = grpMock.Object;
        _project.ItemNames = [];
        _project.Items.Add(new BackgroundItem("BG_ROUD2", 0x4C, _bgTableFile.BgTableEntries[0x4C], _project));
        _project.ItemNames.Add("BG_ROUD2", "BG_HALLWAY_WONKY");
        _project.Items.Add(new BackgroundItem("KBG00", 0xB6, _bgTableFile.BgTableEntries[0xB6], _project));
        _project.ItemNames.Add("KBG00", "BG_KBG00");
        Mock<ProjectSettings> settingsMock = new();
        _project.Settings = settingsMock.Object;
    }

    [AvaloniaTest]
    public void ItemExplorerPanel_CanOpenItem()
    {
        int currentFrame = 0;
        MainWindowViewModel mainWindow = new() { OpenProject = _project };
        MainWindow window = new() { DataContext = mainWindow, ConfigurationFactory = new ConfigFactoryMock("config.json") };
        window.Show();
        mainWindow.Window = window;
        mainWindow.OpenProjectView(_project, new TestProgressTracker());
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        TreeDataGrid viewer = ((OpenProjectPanel)mainWindow.Window.MainContent.Content)?.ItemExplorer.Viewer;
        viewer!.RowSelection!.Select(new(0, 1));
        mainWindow.ItemExplorer.OpenItemCommand.Execute(viewer);
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        Assert.That(mainWindow.EditorTabs.Tabs, Has.Count.EqualTo(1));
    }

    [AvaloniaTest]
    public void ItemExplorerPanel_CanRenameItem()
    {
        int currentFrame = 0;
        MainWindowViewModel mainWindow = new() { OpenProject = _project };
        MainWindow window = new() { DataContext = mainWindow, ConfigurationFactory = new ConfigFactoryMock("config.json") };
        window.Show();
        mainWindow.Window = window;
        mainWindow.OpenProjectView(_project, new TestProgressTracker());
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        int topOffset = OperatingSystem.IsMacOS() ? 85 : 70;
        TreeDataGrid viewer = ((OpenProjectPanel)mainWindow.Window.MainContent.Content)!.ItemExplorer.Viewer;
        window.MouseDown(new(21, viewer.Bounds.Top + mainWindow.Window.ToolBar.Bounds.Bottom + topOffset), MouseButton.Left);
        window.MouseUp(new(21, viewer.Bounds.Top + mainWindow.Window.ToolBar.Bounds.Bottom + topOffset), MouseButton.Left);
        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, "ArrowRight");
        window.MouseDown(new(80, viewer.Bounds.Top + mainWindow.Window.ToolBar.Bounds.Bottom + topOffset + 19), MouseButton.Left);
        window.MouseUp(new(80, viewer.Bounds.Top + mainWindow.Window.ToolBar.Bounds.Bottom + topOffset + 19), MouseButton.Left);
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        mainWindow.ItemExplorer.OpenItemCommand.Execute(viewer);
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        Assert.That(((ITreeItem)viewer.RowSelection!.SelectedItem)!.Text, Is.EqualTo(mainWindow.EditorTabs.Tabs[0].Description.DisplayName));

        window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, "F2");
        if (OperatingSystem.IsMacOS())
        {
            window.KeyPress(Key.A, RawInputModifiers.Meta, PhysicalKey.A, "Command+A");
        }
        else
        {
            window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "Ctrl+A");
        }
        window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.F2, "F2");
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        window.KeyTextInput("NewName");
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        Assert.Multiple(() =>
        {
            Assert.That(((ITreeItem)viewer.RowSelection!.SelectedItem)!.Text, Is.EqualTo("NewName"));
            Assert.That(mainWindow.EditorTabs.Tabs[0].Description.DisplayName, Is.EqualTo("NewName"));
        });
    }
}
