using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Factories;
using SerialLoops.Tests.Shared;

namespace SerialLoops.Tests.Headless.Panels;

public class HomePanelTests
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

    [SetUp]
    public void Setup()
    {
        string testId = Guid.NewGuid().ToString();
        ConfigFactoryMock configFactory = new($"{testId}.json");
        Config config = configFactory.LoadConfig(s => s, _log);
        config.UserDirectory = Path.Combine(Path.GetTempPath(), testId);
    }
}
