using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Moq;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Tests.Shared;
using SerialLoops.Utility;
using SerialLoops.ViewModels;

namespace SerialLoops.Tests.Headless.Panels;

public class HomePanelTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;
    private MainWindowViewModel _mainWindowVm;
    private Dictionary<string, Project> _projects = [];

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
        Mock<MainWindowViewModel> windowMock = new();
        _mainWindowVm = windowMock.Object;

        _mainWindowVm.Window = new() { DataContext = _mainWindowVm, ConfigurationFactory = configFactory };
        _mainWindowVm.Window.Show();
        _mainWindowVm.CurrentConfig.UserDirectory = Path.Combine(Path.GetTempPath(), testId);

        Mock<LoopyLogger> loggerMock = new();
        loggerMock.Setup(l => l.Log(It.IsAny<string>())).Verifiable();
        _mainWindowVm.Log = loggerMock.Object;

        string[] projectNames = ["TestProject", "RecentProject", "MostRecentProject"];

        foreach (string projectName in projectNames)
        {
            Directory.CreateDirectory(Path.Combine(_mainWindowVm.CurrentConfig.ProjectsDirectory, projectName));
            Mock<Project> projectMock = new();
            projectMock.SetupCommonMocks();
            Project project = projectMock.Object;
            project.SetupMockedProperties(_uiVals);
            project.Config = _mainWindowVm.CurrentConfig;
            project.Name = projectName;
            project.Items = [];
            project.Save(_log);
            _projects.Add(projectName, project);
        }

        _mainWindowVm.ProjectsCache = new()
        {
            CacheFilePath = Path.Combine(_mainWindowVm.CurrentConfig.UserDirectory, "projects-cache.json"),
            RecentProjects = [_projects["MostRecentProject"].ProjectFile, _projects["RecentProject"].ProjectFile],
            RecentWorkspaces = new() { { _projects["MostRecentProject"].ProjectFile, new() }, { _projects["RecentProject"].ProjectFile, new() } },
        };
        _mainWindowVm.ProjectsCache.Save(_log);
        _mainWindowVm.WindowMenu = new() { { MenuHeader.TOOLS, new() } };
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_mainWindowVm.CurrentConfig.UserDirectory, true);
    }

    [AvaloniaTest]
    public void HomePanel_OpeningProjectAddsToRecentProjects()
    {
        _mainWindowVm.HomePanel = new(_mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.ProjectsCache.RecentProjects, Has.Count.EqualTo(2));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects, Has.Count.EqualTo(2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo(Path.GetFileName(_mainWindowVm.ProjectsCache.RecentProjects[0])));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo(Path.GetFileName(_mainWindowVm.ProjectsCache.RecentProjects[1])));
        });

        _mainWindowVm.OpenProject = _projects["TestProject"];
        _mainWindowVm.OpenProjectView(_projects["TestProject"], new TestProgressTracker());
        Assert.That(_mainWindowVm.ProjectsCache.RecentProjects, Has.Count.EqualTo(3));
        _mainWindowVm.CloseProjectCommand.Execute(null);

        Assert.That(_mainWindowVm.HomePanel.RecentProjects, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("TestProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo(Path.GetFileName(_mainWindowVm.ProjectsCache.RecentProjects[0])));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo(Path.GetFileName(_mainWindowVm.ProjectsCache.RecentProjects[1])));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[2].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[2].Text, Is.EqualTo(Path.GetFileName(_mainWindowVm.ProjectsCache.RecentProjects[2])));
        });
    }

    [AvaloniaTest]
    public void HomePanel_CanRename()
    {
        _mainWindowVm.HomePanel = new(_mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
        });
        string oldFile = _mainWindowVm.ProjectsCache.RecentProjects[0];

        _mainWindowVm.HomePanel.RecentProjects[0].RenameCommand.Execute(null);
        _mainWindowVm.Window.KeyTextInput("BetterProject");
        _mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("BetterProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(File.Exists(_mainWindowVm.ProjectsCache.RecentProjects[0]), Is.True);
            Assert.That(File.Exists(oldFile), Is.False);
        });
    }

    [AvaloniaTest]
    public void HomePanel_CannotRenameToExisting()
    {
        _mainWindowVm.HomePanel = new(_mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
        });
        string oldFile = _mainWindowVm.ProjectsCache.RecentProjects[0];

        _mainWindowVm.HomePanel.RecentProjects[0].RenameCommand.Execute(null);
        _mainWindowVm.Window.KeyTextInput("RecentProject");
        _mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(_mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(File.Exists(oldFile), Is.True);
        });
    }
}
