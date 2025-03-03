using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using Moq;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Editors;
using SerialLoops.Views;
using SerialLoops.Views.Panels;
using Tabalonia.Controls;

namespace SerialLoops.Tests.Headless.Panels;

[TestFixture]
public class EditorTabsPanelTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;
    private Project _project;

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
        _project = projectMock.Object;
        _project.SetupMockedProperties(_uiVals);
        _project.Extra = _extra;
        Mock<ArchiveFile<GraphicsFile>> grpMock = new();
        grpMock.SetupGrpMock(_uiVals.AssetsDirectory, _log);
        _project.Grp = grpMock.Object;
        _project.Items.Add(new BackgroundItem("BG_ROUD2", 0x4C, _bgTableFile.BgTableEntries[0x4C], _project));
        _project.Items.Add(new BackgroundItem("KBG00", 0xB6, _bgTableFile.BgTableEntries[0xB6], _project));
    }

    [AvaloniaTest]
    public void EditorTabsPanel_CanOpenTab()
    {
        int currentFrame = 0;
        MainWindowViewModel mainWindow = new() { OpenProject = _project };
        MainWindow window = new() { DataContext = mainWindow, ConfigurationFactory = new ConfigFactoryMock("config.json") };
        window.Show();
        mainWindow.Window = window;
        mainWindow.OpenProjectView(_project, new TestProgressTracker());
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        mainWindow.EditorTabs.OpenTab(_project.Items[0]);
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        Assert.Multiple(() =>
        {
            Assert.That(window.FindDescendantOfType<EditorTabsPanel>()!.Tabs.Items, Has.Count.EqualTo(1));
            Assert.That(mainWindow.EditorTabs.Tabs[0], Is.TypeOf<BackgroundEditorViewModel>());
        });
    }

    [AvaloniaTest]
    public void EditorTabsPanel_MiddleClickingClosesTab()
    {
        int currentFrame = 0;
        MainWindowViewModel mainWindow = new() { OpenProject = _project };
        MainWindow window = new() { DataContext = mainWindow, ConfigurationFactory = new ConfigFactoryMock("config.json") };
        window.Show();
        mainWindow.Window = window;
        mainWindow.OpenProjectView(_project, new TestProgressTracker());
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        mainWindow.EditorTabs.OpenTab(_project.Items[0]);
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        Assert.That(mainWindow.EditorTabs.Tabs, Has.Count.EqualTo(1));

        EditorTabsPanel editorTabsPanel = mainWindow.Window.FindDescendantOfType<EditorTabsPanel>();
        Assert.That(editorTabsPanel, Is.Not.Null);
        ItemExplorerPanel itemExplorerPanel = window.FindDescendantOfType<ItemExplorerPanel>();
        DragTabItem tab = editorTabsPanel.FindDescendantOfType<DragTabItem>();
        StackPanel header = tab.FindDescendantOfType<StackPanel>();
        window.MouseDown(new(itemExplorerPanel.Width + header.Bounds.Center.X, 105), MouseButton.Middle);
        window.MouseUp(new(itemExplorerPanel.Width + header.Bounds.Center.X, 105), MouseButton.Middle);
        window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        Assert.That(mainWindow.EditorTabs.Tabs, Has.Count.EqualTo(0));
    }
}
