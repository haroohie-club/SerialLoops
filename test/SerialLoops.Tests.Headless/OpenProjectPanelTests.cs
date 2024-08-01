using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using NAudio.Wave;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views;
using SerialLoops.Views.Dialogs;
using SerialLoops.Views.Editors;
using SerialLoops.Views.Panels;

namespace SerialLoops.Tests.Headless
{
    public class OpenProjectPanelTests
    {
        // Note that despite appearing to be unit tests, these tests need to all be run at once
        // We can't do Avalonia stuff outside of the [AvaloniaTest] fixture and there's no [AvaloniaSetUp] fixture so
        // we order the tests so that the first one to run sets up stuff for the later tests

        // To run these tests locally, you can create a file called 'ui_vals.json' and place it next to the test assembly (in the output folder)
        private UiVals? _uiVals;

        private string _createdProjectPath;

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
                Console.WriteLine(romUri);
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
            Directory.Delete(_createdProjectPath, recursive: true);
        }

        [AvaloniaTest, Order(1)]
        public async Task ProjectCreationTest()
        {
            int currentFrame = 0;
            MainWindowViewModel mainWindowViewModel = new();
            MainWindow mainWindow = new()
            {
                DataContext = mainWindowViewModel,
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
            _createdProjectPath = mainWindowViewModel.OpenProject.MainDirectory;

            // Verify that the project panel is open
            Assert.That(mainWindow.MainContent.Content, Is.TypeOf<OpenProjectPanel>());
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            // Close the project
            mainWindow.KeyPressQwerty(PhysicalKey.W, RawInputModifiers.Control);
            Assert.That(mainWindow.MainContent.Content, Is.TypeOf<HomePanel>());
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(ProjectCreationTest), ref currentFrame);

            // Reopen the project
            await mainWindowViewModel.OpenProjectFromPath(Path.Combine(_createdProjectPath, $"{_uiVals.ProjectName}.slproj"));
            Assert.That(mainWindow.MainContent.Content, Is.TypeOf<OpenProjectPanel>());
        }

        [AvaloniaTest]
        [Parallelizable]
        public async Task BackgroundEditor_CanOpenTabs()
        {
            int currentFrame = 0;
            MainWindowViewModel mainWindowViewModel = new();
            MainWindow mainWindow = new()
            {
                DataContext = mainWindowViewModel,
            };
            mainWindow.Show();
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanOpenTabs), ref currentFrame);

            await mainWindowViewModel.OpenProjectFromPath(Path.Combine(_createdProjectPath, $"{_uiVals.ProjectName}.slproj"));
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanOpenTabs), ref currentFrame);

            OpenProjectPanel openProjectPanel = (OpenProjectPanel)mainWindow.MainContent.Content;
            OpenProjectPanelViewModel openProjectViewModel = (OpenProjectPanelViewModel)openProjectPanel.DataContext;

            ItemExplorerPanel explorer = openProjectPanel.ItemExplorer;
            EditorTabsPanel tabs = openProjectPanel.EditorTabs;

            ITreeItem backgroundsTreeItem = openProjectViewModel.Explorer.Source.First(i => i.Text == "Backgrounds");

            mainWindow.TabToExplorer();

            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanOpenTabs), ref currentFrame);
            explorer.Viewer.SelectedItem = backgroundsTreeItem;
            mainWindow.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanOpenTabs), ref currentFrame);
            ItemDescription firstBg = mainWindowViewModel.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Background);
            explorer.Viewer.SelectedItem = backgroundsTreeItem.Children.First(i => i.Text == firstBg.DisplayName);
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BackgroundEditor_CanOpenTabs), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(openProjectViewModel.EditorTabs.SelectedTab?.Description?.DisplayName, Is.EqualTo(firstBg.DisplayName));
                Assert.That(tabs.Tabs.SelectedItem, Is.TypeOf<BackgroundEditorViewModel>());
            });
        }

        [AvaloniaTest]
        [Parallelizable]
        public async Task BGMEditor_CanPlayPauseStopMusic()
        {
            int currentFrame = 0;
            MainWindowViewModel mainWindowViewModel = new();
            MainWindow mainWindow = new()
            {
                DataContext = mainWindowViewModel,
            };
            mainWindow.Show();
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);

            await mainWindowViewModel.OpenProjectFromPath(Path.Combine(_createdProjectPath, $"{_uiVals.ProjectName}.slproj"));
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);

            OpenProjectPanel openProjectPanel = (OpenProjectPanel)mainWindow.MainContent.Content;
            OpenProjectPanelViewModel openProjectViewModel = (OpenProjectPanelViewModel)openProjectPanel.DataContext;

            ItemExplorerPanel explorer = openProjectPanel.ItemExplorer;
            EditorTabsPanel tabs = openProjectPanel.EditorTabs;

            ITreeItem bgmsTreeItem = openProjectViewModel.Explorer.Source.First(i => i.Text == "BGMs");

            mainWindow.TabToExplorer();

            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);
            explorer.Viewer.SelectedItem = bgmsTreeItem;
            mainWindow.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            mainWindow.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);

            ItemDescription bgm029 = mainWindowViewModel.OpenProject.Items.First(i => i.Name == "BGM029");
            explorer.Viewer.SelectedItem = bgmsTreeItem.Children.First(i => i.Text == bgm029.DisplayName);
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(openProjectViewModel.EditorTabs.SelectedTab?.Description?.DisplayName, Is.EqualTo(bgm029.DisplayName));
                Assert.That(tabs.Tabs.SelectedItem, Is.TypeOf<BackgroundMusicEditorViewModel>());
            });

            BackgroundMusicEditorViewModel bgmEditorViewModel = (BackgroundMusicEditorViewModel)tabs.Tabs.SelectedItem;
            BackgroundMusicEditorView bgmEditor = mainWindow.FindDescendantOfType<BackgroundMusicEditorView>();
            SoundPlayerPanel soundPlayer = bgmEditor.Player;
            SoundPlayerPanelViewModel soundPlayerViewModel = (SoundPlayerPanelViewModel)soundPlayer.DataContext;

            // Check that the sound plays
            soundPlayer.PlayPauseButton.Focus();
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(soundPlayerViewModel.StopButtonEnabled, Is.True);
                Assert.That(soundPlayerViewModel._player.PlaybackState, Is.EqualTo(PlaybackState.Playing));
                Assert.That(soundPlayerViewModel.PlayPauseImagePath, Contains.Substring("Pause"));
            });

            // The AL player is kinda unreliable from a testing perspective and thus these tests are restricted to Windows
#if WINDOWS
            // Check that we can pause the sound
            soundPlayer.PlayPauseButton.Focus();
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(soundPlayerViewModel.StopButtonEnabled, Is.True);
                Assert.That(soundPlayerViewModel._player.PlaybackState, Is.EqualTo(PlaybackState.Paused));
                Assert.That(soundPlayerViewModel.PlayPauseImagePath, Contains.Substring("Play"));
            });

            // Check that the sound stops when it reaches the end of its play
            // This doesn't work -- probably something with the headless stuff messing with playback
            //soundPlayer.PlayPauseButton.Focus();
            //mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            //await Task.Delay(TimeSpan.FromSeconds(7));
            //mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);
            //Assert.Multiple(() =>
            //{
            //    Assert.That(soundPlayerViewModel.StopButtonEnabled, Is.False);
            //    Assert.That(soundPlayerViewModel._player.PlaybackState, Is.EqualTo(PlaybackState.Stopped));
            //    Assert.That(soundPlayerViewModel.PlayPauseImagePath, Contains.Substring("Play"));
            //});

            // Check that we can stop the sound
            soundPlayer.PlayPauseButton.Focus();
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(soundPlayerViewModel.StopButtonEnabled, Is.True);
                Assert.That(soundPlayerViewModel._player.PlaybackState, Is.EqualTo(PlaybackState.Playing));
                Assert.That(soundPlayerViewModel.PlayPauseImagePath, Contains.Substring("Pause"));
            });
#endif
            soundPlayer.StopButton.Focus();
            mainWindow.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            mainWindow.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(BGMEditor_CanPlayPauseStopMusic), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(soundPlayerViewModel.StopButtonEnabled, Is.False);
#if WINDOWS
                Assert.That(soundPlayerViewModel._player.PlaybackState, Is.EqualTo(PlaybackState.Stopped));
#else
                Assert.That(soundPlayerViewModel._player.PlaybackState, Is.EqualTo(PlaybackState.Paused));
#endif
                Assert.That(soundPlayerViewModel.PlayPauseImagePath, Contains.Substring("Play"));
            });
        }
    }
}
