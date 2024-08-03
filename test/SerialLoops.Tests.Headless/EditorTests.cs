using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views;
using SerialLoops.Views.Dialogs;
using SerialLoops.Views.Panels;
using Tabalonia.Controls;

namespace SerialLoops.Tests.Headless
{
    public class EditorTests
    {
        // To run these tests locally, you can create a file called 'ui_vals.json' and place it next to the test assembly (in the output folder)
        private UiVals? _uiVals;

        [OneTimeSetUp]
        public void Setup()
        {
            if (File.Exists("ui_vals.json"))
            {
                _uiVals = JsonSerializer.Deserialize<UiVals>(File.ReadAllText("ui_vals.json")) ?? new();
                if (!Directory.Exists(_uiVals.ArtifactsDir))
                {
                    Directory.CreateDirectory(_uiVals.ArtifactsDir);
                }
            }
            else
            {
                string romUri = Environment.GetEnvironmentVariable(UiVals.ROM_URI_ENV_VAR) ?? string.Empty;
                string romPath = Path.Combine(Directory.GetCurrentDirectory(), UiVals.ROM_NAME);
                HttpClient httpClient = new();
                using Stream downloadStream = httpClient.Send(new() { Method = HttpMethod.Get, RequestUri = new(romUri) }).Content.ReadAsStream();
                using FileStream fileStream = new(romPath, FileMode.Create);
                downloadStream.CopyTo(fileStream);
                fileStream.Flush();

                _uiVals = new()
                {
                    ProjectName = Environment.GetEnvironmentVariable(UiVals.PROJECT_NAME_ENV_VAR) ?? "HeadlessUITests",
                    RomLoc = romPath,
                    ArtifactsDir = Environment.GetEnvironmentVariable(UiVals.ARTIFACTS_DIR_ENV_VAR) ?? "artifacts",
                };
            }
        }

        [AvaloniaTest]
        public async Task ProjectCreationTest()
        {
            int currentFrame = 0;
            ConfigFactoryMock configFactory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config-{nameof(ProjectCreationTest)}.json"));
            MainWindowViewModel mainWindowViewModel = new();
            MainWindow mainWindow = new()
            {
                DataContext = mainWindowViewModel,
                ConfigurationFactory = configFactory,
            };
            mainWindow.Show();
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            ProjectCreationDialogViewModel projectCreationViewModel = new(mainWindowViewModel.CurrentConfig, mainWindowViewModel, mainWindowViewModel.Log);
            ProjectCreationDialog projectCreationDialog = new()
            {
                DataContext = projectCreationViewModel
            };
            projectCreationDialog.Show();
            projectCreationDialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            projectCreationDialog.NameBox.Focus();
            projectCreationDialog.KeyTextInput(_uiVals.ProjectName);
            projectCreationDialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            projectCreationDialog.LanguageComboBox.Focus();
            projectCreationDialog.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            projectCreationDialog.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            projectCreationDialog.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            projectCreationDialog.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            projectCreationDialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            projectCreationViewModel.RomPath = _uiVals!.RomLoc;
            projectCreationDialog.CreateButton.Focus();
            projectCreationDialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            await Task.Delay(TimeSpan.FromSeconds(15)); // Give us time for project creation to complete
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            await mainWindowViewModel.OpenProjectFromPath(Path.Combine(mainWindowViewModel.CurrentConfig.ProjectsDirectory, _uiVals.ProjectName, $"{_uiVals.ProjectName}.slproj"));
            string createdProjectPath = mainWindowViewModel.OpenProject.MainDirectory;

            // Verify that the project panel is open
            Assert.That(mainWindow.MainContent.Content, Is.TypeOf<OpenProjectPanel>());
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            // Close the project
            mainWindow.KeyPressQwerty(PhysicalKey.W, RawInputModifiers.Control);
            Assert.That(mainWindow.MainContent.Content, Is.TypeOf<HomePanel>());
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            // Reopen the project
            await mainWindowViewModel.OpenProjectFromPath(Path.Combine(createdProjectPath, $"{_uiVals.ProjectName}.slproj"));
            Assert.That(mainWindow.MainContent.Content, Is.TypeOf<OpenProjectPanel>());
            Directory.Delete(createdProjectPath, recursive: true);
        }

        [AvaloniaTest]
        [Parallelizable]
        public async Task BackgroundEditor_CanEditCgNames()
        {
            ConfigFactoryMock configFactory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config-{nameof(BackgroundEditor_CanEditCgNames)}.json"));
            string projectName = $"Headless_{nameof(BackgroundEditor_CanEditCgNames)}";
            int currentFrame = 0;
            MainWindowViewModel mainWindowViewModel = new();
            MainWindow mainWindow = new()
            {
                DataContext = mainWindowViewModel,
                ConfigurationFactory = configFactory,
            };
            mainWindow.Show();
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            Project newProject = new(projectName, "en", mainWindowViewModel.CurrentConfig, (s) => s, mainWindowViewModel.Log);
            TestProgressTracker tracker = new();
            // We're all gonna be trying to access the same ROM at the same time. We should retry if we hit IOExceptions to fix flakiness
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    Lib.IO.OpenRom(newProject, _uiVals.RomLoc, mainWindowViewModel.Log, tracker);
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }
            Assert.That(newProject.Load(mainWindowViewModel.CurrentConfig, mainWindowViewModel.Log, tracker).State, Is.EqualTo(Project.LoadProjectState.SUCCESS));
            mainWindowViewModel.OpenProject = newProject;
            mainWindowViewModel.OpenProjectView(newProject, tracker);
            string createdProjectPath = mainWindowViewModel.OpenProject.MainDirectory;
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);

            OpenProjectPanel openProjectPanel = (OpenProjectPanel)mainWindow.MainContent.Content;
            OpenProjectPanelViewModel openProjectViewModel = (OpenProjectPanelViewModel)openProjectPanel.DataContext;

            ItemExplorerPanel explorer = openProjectPanel.ItemExplorer;
            EditorTabsPanel tabs = openProjectPanel.EditorTabs;

            ITreeItem backgroundsTreeItem = openProjectViewModel.Explorer.Source.First(i => i.Text == "Backgrounds");

            mainWindow.TabToExplorer();

            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            explorer.Viewer.SelectedItem = backgroundsTreeItem;
            mainWindow.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            ItemDescription firstBg = mainWindowViewModel.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Background);
            explorer.Viewer.SelectedItem = backgroundsTreeItem.Children.First(i => i.Text == firstBg.DisplayName);
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(openProjectViewModel.EditorTabs.SelectedTab?.Description?.DisplayName, Is.EqualTo(firstBg.DisplayName));
                Assert.That(tabs.Tabs.SelectedItem, Is.TypeOf<BackgroundEditorViewModel>());
            });

            // Time to select a CG
            ItemDescription firstCg = mainWindowViewModel.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_CG);
            explorer.Viewer.SelectedItem = backgroundsTreeItem.Children.First(i => i.Text == firstCg.DisplayName);
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(openProjectViewModel.EditorTabs.SelectedTab?.Description?.DisplayName, Is.EqualTo(firstCg.DisplayName));
                Assert.That(tabs.Tabs.SelectedItem, Is.TypeOf<BackgroundEditorViewModel>());
            });

            TextBox cgNameBox = tabs.FindLogicalDescendantOfType<TextBox>();
            cgNameBox.Focus();
            string myExWifeStillMissesMe = "...but her aim is getting better!";
            mainWindow.KeyTextInput(myExWifeStillMissesMe);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            bool foundTab = false;
            TextBlock tabTitleBlock = null;
            foreach (ILogical logical in tabs.GetLogicalDescendants())
            {
                if (logical is DragTabItem t && !foundTab)
                {
                    foundTab = true;
                    continue;
                }
                else if (logical is DragTabItem)
                {
                    tabTitleBlock = logical.FindLogicalDescendantOfType<TextBlock>();
                    break;
                }
            }
            string extraFile = Path.Combine(mainWindowViewModel.OpenProject.IterativeDirectory, "assets", "data", $"{mainWindowViewModel.OpenProject.Extra.Index:X3}.s");
            Assert.Multiple(() =>
            {
                Assert.That(tabTitleBlock?.Text, Is.EqualTo($"* {firstCg.DisplayName}"));
                Assert.That(firstCg.UnsavedChanges, Is.True);
                Assert.That(((BackgroundItem)firstCg).CgName, Contains.Substring(myExWifeStillMissesMe));
                Assert.That(extraFile, Does.Not.Exist);
            });

            mainWindow.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.Control); // Save
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(tabTitleBlock?.Text, Is.EqualTo(firstCg.DisplayName));
                Assert.That(firstCg.UnsavedChanges, Is.False);
                Assert.That(((BackgroundItem)firstCg).CgName, Contains.Substring(myExWifeStillMissesMe));
                Assert.That(extraFile, Does.Exist);
                Assert.That(File.ReadAllText(extraFile), Contains.Substring(myExWifeStillMissesMe.GetOriginalString(mainWindowViewModel.OpenProject).EscapeShiftJIS()));
            });

            Directory.Delete(createdProjectPath, recursive: true);
        }
    }
}
