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

    private (MainWindowViewModel, Dictionary<string, Project>) Setup()
    {
        string testId = Guid.NewGuid().ToString();
        ConfigUser configUser = new() { UserDirectory = Path.Combine(Path.GetTempPath(), testId) };

        ConfigFactoryMock configFactory = new($"{testId}.json", configUser);
        Mock<MainWindowViewModel> windowMock = new();
        MainWindowViewModel mainWindowVm = windowMock.Object;

        Mock<LoopyLogger> loggerMock = new();
        loggerMock.Setup(l => l.Log(It.IsAny<string>())).Verifiable();
        mainWindowVm.Log = loggerMock.Object;

        mainWindowVm.Window = new() { DataContext = mainWindowVm, ConfigurationFactory = configFactory };
        mainWindowVm.Window.Show();

        string[] projectNames = ["TestProject", "RecentProject", "MostRecentProject"];

        Dictionary<string, Project> projects = [];
        foreach (string projectName in projectNames)
        {
            Directory.CreateDirectory(Path.Combine(mainWindowVm.CurrentConfig.ProjectsDirectory, projectName));
            Mock<Project> projectMock = new();
            projectMock.SetupCommonMocks();
            Project project = projectMock.Object;
            project.SetupMockedProperties(_uiVals);
            project.ConfigUser = mainWindowVm.CurrentConfig;
            project.Name = projectName;
            project.Items = [];
            project.Save(_log);
            projects.Add(projectName, project);
        }

        mainWindowVm.ProjectsCache = new()
        {
            CacheFilePath = Path.Combine(mainWindowVm.CurrentConfig.UserDirectory, "projects-cache.json"),
            RecentProjects = [projects["MostRecentProject"].ProjectFile, projects["RecentProject"].ProjectFile],
            RecentWorkspaces = new() { { projects["MostRecentProject"].ProjectFile, new() }, { projects["RecentProject"].ProjectFile, new() } },
        };
        mainWindowVm.ProjectsCache.Save(_log);
        mainWindowVm.WindowMenu = new() { { MenuHeader.TOOLS, new() } };

        return (mainWindowVm, projects);
    }

    private void TearDown(MainWindowViewModel mainWindowVm)
    {
        if (mainWindowVm.CurrentConfig.UserDirectory !=
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"))
        {
            Directory.Delete(mainWindowVm.CurrentConfig.UserDirectory, true);
        }
    }

    [AvaloniaTest]
    public void HomePanel_OpeningProjectAddsToRecentProjects()
    {
        (MainWindowViewModel mainWindowVm, Dictionary<string, Project> projects) = Setup();

        mainWindowVm.HomePanel = new(mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.ProjectsCache.RecentProjects, Has.Count.EqualTo(2));
            Assert.That(mainWindowVm.HomePanel.RecentProjects, Has.Count.EqualTo(2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo(Path.GetFileName(mainWindowVm.ProjectsCache.RecentProjects[0])));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo(Path.GetFileName(mainWindowVm.ProjectsCache.RecentProjects[1])));
        });

        mainWindowVm.OpenProject = projects["TestProject"];
        mainWindowVm.OpenProjectView(projects["TestProject"], new TestProgressTracker());
        Assert.That(mainWindowVm.ProjectsCache.RecentProjects, Has.Count.EqualTo(3));
        mainWindowVm.CloseProjectCommand.Execute(null);

        Assert.That(mainWindowVm.HomePanel.RecentProjects, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("TestProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo(Path.GetFileName(mainWindowVm.ProjectsCache.RecentProjects[0])));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo(Path.GetFileName(mainWindowVm.ProjectsCache.RecentProjects[1])));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[2].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[2].Text, Is.EqualTo(Path.GetFileName(mainWindowVm.ProjectsCache.RecentProjects[2])));
        });

        TearDown(mainWindowVm);
    }

    [AvaloniaTest]
    public void HomePanel_CanRename()
    {
        (MainWindowViewModel mainWindowVm, _) = Setup();

        mainWindowVm.HomePanel = new(mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
        });
        string oldFile = mainWindowVm.ProjectsCache.RecentProjects[0];

        mainWindowVm.HomePanel.RecentProjects[0].RenameCommand.Execute(null);
        mainWindowVm.Window.KeyTextInput("BetterProject");
        mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("BetterProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(File.Exists(mainWindowVm.ProjectsCache.RecentProjects[0]), Is.True);
            Assert.That(File.Exists(oldFile), Is.False);
        });

        TearDown(mainWindowVm);
    }

    [AvaloniaTest]
    public void HomePanel_CannotRenameToExisting()
    {
        (MainWindowViewModel mainWindowVm, _) = Setup();

        mainWindowVm.HomePanel = new(mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
        });
        string oldFile = mainWindowVm.ProjectsCache.RecentProjects[0];

        mainWindowVm.HomePanel.RecentProjects[0].RenameCommand.Execute(null);
        mainWindowVm.Window.KeyTextInput("RecentProject");
        mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(File.Exists(oldFile), Is.True);
        });

        TearDown(mainWindowVm);
    }

    [AvaloniaTest]
    public void HomePanel_CanDuplicate()
    {
        (MainWindowViewModel mainWindowVm, _) = Setup();

        mainWindowVm.HomePanel = new(mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
        });
        string oldFile = mainWindowVm.ProjectsCache.RecentProjects[0];

        mainWindowVm.HomePanel.RecentProjects[0].DuplicateCommand.Execute(null);
        mainWindowVm.Window.KeyTextInput("TheBestProjectEver");
        mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(File.Exists(oldFile), Is.True);
            Assert.That(File.Exists(Path.Combine(mainWindowVm.CurrentConfig.ProjectsDirectory, "TheBestProjectEver", "TheBestProjectEver.slproj")), Is.True);
        });

        TearDown(mainWindowVm);
    }

    [AvaloniaTest]
#if !WINDOWS
    [Ignore("Works on Unix platforms in real scenarios but not in headless for some reason")]
#endif
    public void HomePanel_CanDelete()
    {
        (MainWindowViewModel mainWindowVm, _) = Setup();

        mainWindowVm.HomePanel = new(mainWindowVm);
        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("MostRecentProject.slproj"));
            Assert.That(mainWindowVm.HomePanel.RecentProjects[1].Text, Is.EqualTo("RecentProject.slproj"));
        });
        string oldFile = mainWindowVm.ProjectsCache.RecentProjects[0];

        mainWindowVm.HomePanel.RecentProjects[0].DeleteCommand.Execute(null);
        mainWindowVm.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");

        Assert.Multiple(() =>
        {
            Assert.That(mainWindowVm.HomePanel.RecentProjects[0].Text, Is.EqualTo("RecentProject.slproj"));
            Assert.That(File.Exists(oldFile), Is.False);
        });

        TearDown(mainWindowVm);
    }
}
