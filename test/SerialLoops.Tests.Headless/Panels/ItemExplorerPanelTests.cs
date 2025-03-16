using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Tests.Shared;
using SerialLoops.Utility;
using SerialLoops.ViewModels;

namespace SerialLoops.Tests.Headless.Panels;

public class ItemExplorerPanelTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;

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
    }

    public void Setup()
    {
        string testId = Guid.NewGuid().ToString();
        Config config = new() { UserDirectory = Path.Combine(Path.GetTempPath(), testId) };

        ConfigFactoryMock configFactory = new($"{testId}.json", config);
        Mock<MainWindowViewModel> windowMock = new();
        MainWindowViewModel mainWindowVm = windowMock.Object;

        Mock<LoopyLogger> loggerMock = new();
        loggerMock.Setup(l => l.Log(It.IsAny<string>())).Verifiable();
        mainWindowVm.Log = loggerMock.Object;

        mainWindowVm.Window = new() { DataContext = mainWindowVm, ConfigurationFactory = configFactory };
        mainWindowVm.Window.Show();
    }
}
