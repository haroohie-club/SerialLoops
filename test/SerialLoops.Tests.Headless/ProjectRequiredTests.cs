using System;
using System.Collections.Generic;
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
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
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
    public class ProjectRequiredTests
    {
        // To run these tests locally, you can create a file called 'ui_vals.json' and place it next to the test assembly (in the output folder)
        private UiVals? _uiVals;
        private List<string> _dirsToDelete = [];

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

        [OneTimeTearDown]
        public void TearDown()
        {
            foreach (string dir in _dirsToDelete)
            {
                Directory.Delete(dir, true);
            }
        }

        private static (int Index, ITreeItem Item) GetTreeItemRowIndex(OpenProjectPanelViewModel openProjectViewModel, string itemName)
        {
            ITreeItem item = (ITreeItem)openProjectViewModel.Explorer.Source.Rows.FirstOrDefault(i => ((ITreeItem)i.Model).Text == itemName).Model;
            return (openProjectViewModel.Explorer.Source.Rows.ToList().FindIndex(i => ((ITreeItem)i.Model).Text == itemName), item);
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
            _dirsToDelete.Add(createdProjectPath);

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
            _dirsToDelete.Add(mainWindowViewModel.OpenProject.MainDirectory);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);

            OpenProjectPanel openProjectPanel = (OpenProjectPanel)mainWindow.MainContent.Content;
            OpenProjectPanelViewModel openProjectViewModel = (OpenProjectPanelViewModel)openProjectPanel.DataContext;

            ItemExplorerPanel explorer = openProjectPanel.ItemExplorer;
            EditorTabsPanel tabs = openProjectPanel.EditorTabs;

            (int backgroundsTreeItemIndex, ITreeItem backgroundTreeItem) = GetTreeItemRowIndex(openProjectViewModel, Strings.Backgrounds);

            mainWindow.TabToExplorer();

            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            explorer.Viewer.RowSelection.Select(backgroundsTreeItemIndex);
            mainWindow.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            ItemDescription firstBg = mainWindowViewModel.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Background);
            explorer.Viewer.RowSelection.Select(new(backgroundsTreeItemIndex, backgroundTreeItem.Children.ToList().FindIndex(i => i.Text.Equals(firstBg.DisplayName))));
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanEditCgNames), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(openProjectViewModel.EditorTabs.SelectedTab?.Description?.DisplayName, Is.EqualTo(firstBg.DisplayName));
                Assert.That(tabs.Tabs.SelectedItem, Is.TypeOf<BackgroundEditorViewModel>());
            });

            // Time to select a CG
            ItemDescription firstCg = mainWindowViewModel.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_CG);
            explorer.Viewer.RowSelection.Select(new(backgroundsTreeItemIndex, backgroundTreeItem.Children.ToList().FindIndex(i => i.Text.Equals(firstCg.DisplayName))));
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
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
        }

        [AvaloniaTest]
        //[TestCase("SaveButton")]
        [TestCase("CancelButton")]
        [Parallelizable(ParallelScope.All)]
        public async Task AsmHacksDialog_ApplyTest(string buttonName)
        {
            ConfigFactoryMock configFactory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config-{nameof(AsmHacksDialog_ApplyTest)}_{buttonName}.json"));
            TestConsoleLogger log = new();
            string projectName = $"Headless_{nameof(AsmHacksDialog_ApplyTest)}_{buttonName}";
            Config config = configFactory.LoadConfig(s => s, log);
            config.UseDocker = true;
            int currentFrame = 0;
            Project project = new(projectName, "en", config, (s) => s, log);
            TestProgressTracker tracker = new();
            // We're all gonna be trying to access the same ROM at the same time. We should retry if we hit IOExceptions to fix flakiness
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    Lib.IO.OpenRom(project, _uiVals.RomLoc, log, tracker);
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }
            Assert.That(project.Load(config, log, tracker).State, Is.EqualTo(Project.LoadProjectState.SUCCESS));
            _dirsToDelete.Add(project.MainDirectory);

            AsmHacksDialogViewModel viewModel = new(project, config, log);
            AsmHacksDialog dialog = new(viewModel);
            dialog.Show();
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, $"{nameof(AsmHacksDialog_ApplyTest)}_{buttonName}", ref currentFrame);

            AsmHack skipOpHack = config.Hacks.First(h => h.Name == "Skip OP");
            viewModel.SelectedHack = skipOpHack;
            viewModel.SelectedHack.IsApplied = true;
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, $"{nameof(AsmHacksDialog_ApplyTest)}_{buttonName}", ref currentFrame);

            AsmHack changeOpModeHack = config.Hacks.First(h => h.Name == "Change OP_MODE Chibi");
            viewModel.SelectedHack = changeOpModeHack;
            dialog.DescriptionPanel.FindLogicalDescendantOfType<ComboBox>().SelectedIndex = 2;
            viewModel.SelectedHack.IsApplied = true;
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, $"{nameof(AsmHacksDialog_ApplyTest)}_{buttonName}", ref currentFrame);

            if (buttonName == "SaveButton")
            {
                dialog.SaveButton.Focus();
                dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
                await Task.Delay(TimeSpan.FromSeconds(5));

                Assert.Multiple(() =>
                {
                    Assert.That(skipOpHack.Applied(project), Is.True);
                    Assert.That(changeOpModeHack.Applied(project), Is.True);
                });
            }
            else
            {
                dialog.CancelButton.Focus();
                dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
                await Task.Delay(TimeSpan.FromSeconds(5));

                Assert.Multiple(() =>
                {
                    Assert.That(skipOpHack.Applied(project), Is.False);
                    Assert.That(changeOpModeHack.Applied(project), Is.False);
                });
            }
        }

        [AvaloniaTest]
        [Parallelizable]
        public async Task CharacterSpriteEditor_CanEdit()
        {
            ConfigFactoryMock configFactory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config-{nameof(CharacterSpriteEditor_CanEdit)}.json"));
            string projectName = $"Headless_{nameof(CharacterSpriteEditor_CanEdit)}";
            int currentFrame = 0;
            MainWindowViewModel mainWindowViewModel = new();
            MainWindow mainWindow = new()
            {
                DataContext = mainWindowViewModel,
                ConfigurationFactory = configFactory,
            };
            mainWindow.Show();
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(CharacterSpriteEditor_CanEdit), ref currentFrame);
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
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(CharacterSpriteEditor_CanEdit), ref currentFrame);

            OpenProjectPanel openProjectPanel = (OpenProjectPanel)mainWindow.MainContent.Content;
            OpenProjectPanelViewModel openProjectViewModel = (OpenProjectPanelViewModel)openProjectPanel.DataContext;

            ItemExplorerPanel explorer = openProjectPanel.ItemExplorer;
            EditorTabsPanel tabs = openProjectPanel.EditorTabs;

            (int characterSpritesTreeItemIndex, ITreeItem characterSpriteTreeItem) = GetTreeItemRowIndex(openProjectViewModel, "Character Sprites");

            mainWindow.TabToExplorer();

            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(CharacterSpriteEditor_CanEdit), ref currentFrame);
            explorer.Viewer.RowSelection.Select(characterSpritesTreeItemIndex);
            mainWindow.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(CharacterSpriteEditor_CanEdit), ref currentFrame);
            ItemDescription firstCharacterSprite = mainWindowViewModel.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Character_Sprite);
            explorer.Viewer.RowSelection.Select(new(characterSpritesTreeItemIndex, characterSpriteTreeItem.Children.ToList().FindIndex(i => i.Text.Equals(firstCharacterSprite.DisplayName))));
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(CharacterSpriteEditor_CanEdit), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(openProjectViewModel.EditorTabs.SelectedTab?.Description?.DisplayName, Is.EqualTo(firstCharacterSprite.DisplayName));
                Assert.That(tabs.Tabs.SelectedItem, Is.TypeOf<CharacterSpriteEditorViewModel>());
            });

            // Test edit
            CharacterSpriteEditorViewModel viewModel = (CharacterSpriteEditorViewModel)tabs.Tabs.SelectedItem;
            ComboBox characterBox = tabs.FindLogicalDescendantOfType<ComboBox>();
            characterBox.SelectedIndex++;
            Assert.Multiple(() =>
            {
                Assert.That(viewModel.Character.DisplayName, Is.EqualTo(((CharacterItem)characterBox.SelectedItem).DisplayName));
                Assert.That(viewModel.Description.UnsavedChanges, Is.True);
            });
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(CharacterSpriteEditor_CanEdit), ref currentFrame);
            viewModel.Description.UnsavedChanges = false;

            CheckBox isLargeCheckBox = tabs.FindLogicalDescendantOfType<CheckBox>();
            isLargeCheckBox.Focus();
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsLarge, Is.EqualTo(isLargeCheckBox.IsChecked));
                Assert.That(viewModel.Description.UnsavedChanges, Is.True);
            });
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(CharacterSpriteEditor_CanEdit), ref currentFrame);
        }

        //[AvaloniaTest]
        //[Parallelizable]
        //public async Task ScenarioEditor_CanEdit()
        //{
        //    ConfigFactoryMock configFactory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config-{nameof(ScenarioEditor_CanEdit)}.json"));
        //    string projectName = $"Headless_{nameof(ScenarioEditor_CanEdit)}";
        //    int currentFrame = 0;
        //    MainWindowViewModel mainWindowViewModel = new();
        //    MainWindow mainWindow = new()
        //    {
        //        DataContext = mainWindowViewModel,
        //        ConfigurationFactory = configFactory,
        //    };
        //    mainWindow.Show();
        //    mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ScenarioEditor_CanEdit), ref currentFrame);
        //    Project newProject = new(projectName, "en", mainWindowViewModel.CurrentConfig, (s) => s, mainWindowViewModel.Log);
        //    TestProgressTracker tracker = new();
        //    // We're all gonna be trying to access the same ROM at the same time. We should retry if we hit IOExceptions to fix flakiness
        //    for (int i = 0; i < 100; i++)
        //    {
        //        try
        //        {
        //            Lib.IO.OpenRom(newProject, _uiVals.RomLoc, mainWindowViewModel.Log, tracker);
        //            break;
        //        }
        //        catch (IOException)
        //        {
        //            await Task.Delay(TimeSpan.FromMilliseconds(100));
        //        }
        //    }
        //    Assert.That(newProject.Load(mainWindowViewModel.CurrentConfig, mainWindowViewModel.Log, tracker).State, Is.EqualTo(Project.LoadProjectState.SUCCESS));
        //    mainWindowViewModel.OpenProject = newProject;
        //    mainWindowViewModel.OpenProjectView(newProject, tracker);
        //    string createdProjectPath = mainWindowViewModel.OpenProject.MainDirectory;
        //    mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ScenarioEditor_CanEdit), ref currentFrame);

        //    OpenProjectPanel openProjectPanel = (OpenProjectPanel)mainWindow.MainContent.Content;
        //    OpenProjectPanelViewModel openProjectViewModel = (OpenProjectPanelViewModel)openProjectPanel.DataContext;

        //    ItemExplorerPanel explorer = openProjectPanel.ItemExplorer;
        //    EditorTabsPanel tabs = openProjectPanel.EditorTabs;

        //    ITreeItem scenarioTreeItem = openProjectViewModel.Explorer.Source.(i => i.Text == "Scenario");

        //    mainWindow.TabToExplorer();

        //    mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ScenarioEditor_CanEdit), ref currentFrame);
        //    explorer.Viewer.SelectedItem = scenarioTreeItem;
        //    mainWindow.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        //    mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ScenarioEditor_CanEdit), ref currentFrame);
        //    ItemDescription scnearioItem = mainWindowViewModel.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Scenario);
        //    explorer.Viewer.SelectedItem = scenarioTreeItem.Children.First(i => i.Text == scnearioItem.DisplayName);
        //    mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        //    mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ScenarioEditor_CanEdit), ref currentFrame);
        //    Assert.Multiple(() =>
        //    {
        //        Assert.That(openProjectViewModel.EditorTabs.SelectedTab?.Description?.DisplayName, Is.EqualTo(scnearioItem.DisplayName));
        //        Assert.That(tabs.Tabs.SelectedItem, Is.TypeOf<ScenarioEditorViewModel>());
        //    });
        //}
    }
}
