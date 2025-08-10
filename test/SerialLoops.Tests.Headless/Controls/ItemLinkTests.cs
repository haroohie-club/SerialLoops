using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Moq;
using NUnit.Framework;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Editors;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.Tests.Headless.Controls;

[TestFixture]
public class ItemLinkTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;
    private MainWindowViewModel _mainWindowViewModel;
    private Project _project;

    // TODO: once https://github.com/AvaloniaUI/Avalonia/pull/18306 merges, put this attribute back and remove the direct call
    // [SetUp]
    public async Task Setup()
    {
        _log = new();
        if (Path.Exists("ui_vals.json"))
        {
            _uiVals = JsonSerializer.Deserialize<UiVals>(File.ReadAllText("ui_vals.json"));
        }
        else
        {
            _uiVals = await UiVals.DownloadTestAssets();
        }

        Mock<Project> projectMock = new();
        projectMock.SetupCommonMocks();
        _project = projectMock.Object;
        _project.SetupMockedProperties(_uiVals);

        Mock<MainWindowViewModel> windowMock = new() { CallBase = true };
        windowMock.SetupCommonMocks();
        _mainWindowViewModel = windowMock.Object;
        _mainWindowViewModel.SetupMockedProperties(_project, _log);
        _mainWindowViewModel.CurrentConfig = new ConfigFactoryMock("config.json").LoadConfig(s => s, _log);
        _project.ConfigUser = _mainWindowViewModel.CurrentConfig;
        _project.Name = nameof(ItemLinkTests);
    }

    [AvaloniaTest]
    public async Task ItemLink_ClickOpensTab()
    {
        await Setup();

        EditorTabsPanelViewModel tabs = new(_mainWindowViewModel, _project, _log);
        BackgroundItem bg = new("BG_TEST");
        ItemLink link = new() { Tabs = tabs, Item = bg };
        Window window = new() { Content = new StackPanel { Children = { link } } };
        window.Show();
        Assert.Multiple(() =>
        {
            Assert.That(link.Link.Icon, Is.Not.Null);
            Assert.That(link.Link.Text, Is.EqualTo(bg.DisplayName));
        });
        window.MouseDown(new (30, link.Bounds.Center.Y), MouseButton.Left);
        window.MouseUp(new (30, link.Bounds.Center.Y), MouseButton.Left);
        Assert.That(tabs.Tabs, Has.Count.EqualTo(1));
        Assert.That(tabs.Tabs[0], Is.TypeOf<BackgroundEditorViewModel>());
    }
}
