using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.Tests.Headless
{
    public class DialogTests
    {
        // To run these tests locally, you can create a file called 'ui_vals.json' and place it next to the test assembly (in the output folder)
        private UiVals? _uiVals;

        [OneTimeSetUp]
        public void SetUp()
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
                _uiVals = new()
                {
                    ArtifactsDir = Environment.GetEnvironmentVariable(UiVals.ARTIFACTS_DIR_ENV_VAR) ?? "artifacts",
                };
            }
        }

        [AvaloniaTest]
        [TestCase("SaveButton")]
        [TestCase("CancelButton")]
        [Parallelizable(ParallelScope.All)]
        public void PreferencesDialogTest(string buttonName)
        {
            int currentFrame = 0;

            TestConsoleLogger log = new();
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config-{buttonName}.json");
            ConfigFactoryMock configFactory = new(configPath);
            Config config = configFactory.LoadConfig(s => s, log);

            Strings.Culture = new(config.CurrentCultureName);
            PreferencesDialogViewModel viewModel = new();
            PreferencesDialog dialog = new();
            viewModel.Initialize(dialog, log, configFactory);
            dialog.DataContext = viewModel;

            dialog.Show();

            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            TabItem serialLoopsTab = dialog.FindControl<TabItem>("SerialLoopsTab");
            TabItem projectsTab = dialog.FindControl<TabItem>("ProjectsTab");
            TabItem buildTab = dialog.FindControl<TabItem>("BuildTab");

            LabelWithIcon restartRequiredLabel = dialog.FindControl<LabelWithIcon>("RestartRequiredLabel");

            OptionsGroup serialLoopsGroup = dialog.FindControl<OptionsGroup>("SerialLoopsOptions");
            ComboBox languageComboBox = null;
            ComboBox displayFontComboBox = null;
            CheckBox checkForUpdatesCheckBox = null;
            CheckBox usePreleaseCheckBox = null;
            foreach (ILogical logical in serialLoopsGroup.GetLogicalDescendants())
            {
                if (((Control)logical).Name?.Equals("LanguageControl") ?? false)
                {
                    languageComboBox = logical.FindLogicalDescendantOfType<ComboBox>();
                }
                else if (((Control)logical).Name?.Equals("DisplayFontControl") ?? false)
                {
                    displayFontComboBox = logical.FindLogicalDescendantOfType<ComboBox>();
                }
                else if (((Control)logical).Name?.Equals("CheckforUpdatesonStartupControl") ?? false)
                {
                    checkForUpdatesCheckBox = logical.FindLogicalDescendantOfType<CheckBox>();
                }
                else if (((Control)logical).Name?.Equals("UsePreReleaseUpdateChannelControl") ?? false)
                {
                    usePreleaseCheckBox = logical.FindLogicalDescendantOfType<CheckBox>();
                }
            }

            OptionsGroup projectGroup = dialog.FindControl<OptionsGroup>("ProjectOptions");
            CheckBox autoReOpenProjectCheckBox = null;
            CheckBox rememberWorkspaceCheckBox = null;
            CheckBox removeMissingCheckBox = null;
            foreach (ILogical logical in projectGroup.GetLogicalDescendants())
            {
                if (((Control)logical).Name?.Equals("AutoReOpenLastProjectControl") ?? false)
                {
                    autoReOpenProjectCheckBox = logical.FindLogicalDescendantOfType<CheckBox>();
                }
                else if (((Control)logical).Name?.Equals("RememberProjectWorkspaceControl") ?? false)
                {
                    rememberWorkspaceCheckBox = logical.FindLogicalDescendantOfType<CheckBox>();
                }
                else if (((Control)logical).Name?.Equals("RemoveMissingProjectsControl") ?? false)
                {
                    removeMissingCheckBox = logical.FindLogicalDescendantOfType<CheckBox>();
                }
            }

            OptionsGroup buildGroup = dialog.FindControl<OptionsGroup>("BuildOptions");
            TextBox devkitArmTextBox = null;
            TextBox emulatorPathTextBox = null;
            CheckBox useDockerCheckBox = null;
            TextBox dockerTagTextBox = null;
            foreach (ILogical logical in buildGroup.GetLogicalDescendants())
            {
                if (((Control)logical).Name?.Equals("devkitARMPathControl") ?? false)
                {
                    devkitArmTextBox = logical.FindLogicalDescendantOfType<TextBox>();
                }
                else if (((Control)logical).Name?.Equals("EmulatorPathControl") ?? false)
                {
                    emulatorPathTextBox = logical.FindLogicalDescendantOfType<TextBox>();
                }
                else if (((Control)logical).Name?.Equals("UseDockerforASMHacksControl") ?? false)
                {
                    useDockerCheckBox = logical.FindLogicalDescendantOfType<CheckBox>();
                }
                else if (((Control)logical).Name?.Equals("devkitARMDockerTagControl") ?? false)
                {
                    dockerTagTextBox = logical.FindLogicalDescendantOfType<TextBox>();
                }
            }

            // Assert that all the controls have been populated correctly
            Assert.Multiple(() =>
            {
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
                Assert.That(((ComboBoxItem)languageComboBox.SelectedItem)?.Tag, Is.EqualTo(config.CurrentCultureName));
                Assert.That(((ComboBoxItem)languageComboBox.SelectedItem)?.Content, Is.EqualTo(new CultureInfo(config.CurrentCultureName).NativeName.ToSentenceCase()));
                Assert.That(((string)((ComboBoxItem)displayFontComboBox.SelectedItem)?.Tag).Equals(config.DisplayFont)
                    || (string.IsNullOrEmpty((string)((ComboBoxItem)displayFontComboBox.SelectedItem)?.Tag) == string.IsNullOrEmpty(config.DisplayFont)));
                Assert.That(checkForUpdatesCheckBox.IsChecked, Is.EqualTo(config.CheckForUpdates));
                Assert.That(usePreleaseCheckBox.IsChecked, Is.EqualTo(config.PreReleaseChannel));
                Assert.That(autoReOpenProjectCheckBox.IsChecked, Is.EqualTo(config.AutoReopenLastProject));
                Assert.That(rememberWorkspaceCheckBox.IsChecked, Is.EqualTo(config.RememberProjectWorkspace));
                Assert.That(removeMissingCheckBox.IsChecked, Is.EqualTo(config.RemoveMissingProjects));
                Assert.That(devkitArmTextBox.Text, Is.EqualTo(config.DevkitArmPath));
                Assert.That(emulatorPathTextBox.Text, Is.EqualTo(config.EmulatorPath));
                Assert.That(useDockerCheckBox.IsChecked, Is.EqualTo(config.UseDocker));
                Assert.That(dockerTagTextBox.Text, Is.EqualTo(config.DevkitArmDockerTag));
            });

            languageComboBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(((ComboBoxItem)languageComboBox.SelectedItem).Tag, Is.Not.EqualTo(config.CurrentCultureName));
                Assert.That(((ComboBoxItem)languageComboBox.SelectedItem).Content, Is.Not.EqualTo(new CultureInfo(config.CurrentCultureName).NativeName.ToSentenceCase()));
                Assert.That(restartRequiredLabel.IsVisible, Is.True); // Let's make sure we've alerted users they have to restart
            });

            viewModel.RequireRestart = false; // reset it for the next test
            Assert.That(restartRequiredLabel.IsVisible, Is.False); // Double check the binding happened
            displayFontComboBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(((ComboBoxItem)displayFontComboBox.SelectedItem).Tag, Is.Not.EqualTo(config.DisplayFont));
                Assert.That(restartRequiredLabel.IsVisible, Is.True); // Let's make sure we've alerted users they have to restart
            });

            viewModel.RequireRestart = false; // reset it for the next test
            checkForUpdatesCheckBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(checkForUpdatesCheckBox.IsChecked, Is.Not.EqualTo(config.CheckForUpdates));
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
            });

            usePreleaseCheckBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(usePreleaseCheckBox.IsChecked, Is.Not.EqualTo(config.PreReleaseChannel));
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
            });

            // Time for the projects tab
            serialLoopsTab.Focus();
            dialog.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);

            autoReOpenProjectCheckBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(autoReOpenProjectCheckBox.IsChecked, Is.Not.EqualTo(config.AutoReopenLastProject));
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
            });

            rememberWorkspaceCheckBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(rememberWorkspaceCheckBox.IsChecked, Is.Not.EqualTo(config.RememberProjectWorkspace));
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
            });

            removeMissingCheckBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(removeMissingCheckBox.IsChecked, Is.Not.EqualTo(config.RemoveMissingProjects));
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
            });

            // Time for the build tab
            projectsTab.Focus();
            dialog.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);

            string devkitARMInput = "thank you based devkitPro organization";
            devkitArmTextBox.Focus();
            dialog.KeyTextInput(devkitARMInput);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(devkitArmTextBox.Text, Contains.Substring(devkitARMInput));
                Assert.That(devkitArmTextBox.Text, Is.Not.EqualTo(config.DevkitArmPath));
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
            });

            string emulatorPathInput = "thank you based melonDS devs";
            emulatorPathTextBox.Focus();
            dialog.KeyTextInput(emulatorPathInput);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            Assert.Multiple(() =>
            {
                Assert.That(emulatorPathTextBox.Text, Contains.Substring(emulatorPathInput));
                Assert.That(emulatorPathTextBox.Text, Is.Not.EqualTo(config.EmulatorPath));
                Assert.That(restartRequiredLabel.IsVisible, Is.False);
            });

            useDockerCheckBox.Focus();
            dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            if (OperatingSystem.IsMacOS())
            {
                Assert.Multiple(() =>
                {
                    Assert.That(useDockerCheckBox.IsChecked, Is.EqualTo(config.UseDocker));
                    Assert.That(restartRequiredLabel.IsVisible, Is.False);
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(useDockerCheckBox.IsChecked, Is.Not.EqualTo(config.UseDocker));
                    Assert.That(restartRequiredLabel.IsVisible, Is.False);
                });
            }

            string dockerTagInput = "thank you based docker creators and such";
            dockerTagTextBox.Focus();
            dialog.KeyTextInput(dockerTagInput);
            dialog.CaptureAndSaveFrame(_uiVals!.ArtifactsDir, nameof(PreferencesDialogTest), ref currentFrame);
            if (OperatingSystem.IsMacOS())
            {
                Assert.Multiple(() =>
                {
                    Assert.That(dockerTagTextBox.Text, Does.Not.Contain(dockerTagInput));
                    Assert.That(restartRequiredLabel.IsVisible, Is.False);
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(dockerTagTextBox.Text, Contains.Substring(dockerTagInput));
                    Assert.That(dockerTagTextBox.Text, Is.Not.EqualTo(config.EmulatorPath));
                    Assert.That(restartRequiredLabel.IsVisible, Is.False);
                });
            }

            Button button = dialog.Find<Button>(buttonName);
            button.Focus();
            dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

            Config secondConfig = configFactory.LoadConfig(s => s, log);

            if (buttonName.Equals("SaveButton"))
            {
                Assert.Multiple(() =>
                {
                    Assert.That(config.CurrentCultureName, Is.Not.EqualTo(secondConfig.CurrentCultureName));
                    Assert.That(config.DisplayFont, Is.Not.EqualTo(secondConfig.DisplayFont));
                    Assert.That(config.CheckForUpdates, Is.Not.EqualTo(secondConfig.CheckForUpdates));
                    Assert.That(config.PreReleaseChannel, Is.Not.EqualTo(secondConfig.PreReleaseChannel));
                    Assert.That(config.AutoReopenLastProject, Is.Not.EqualTo(secondConfig.AutoReopenLastProject));
                    Assert.That(config.RememberProjectWorkspace, Is.Not.EqualTo(secondConfig.RememberProjectWorkspace));
                    Assert.That(config.RemoveMissingProjects, Is.Not.EqualTo(secondConfig.RemoveMissingProjects));
                    Assert.That(config.DevkitArmPath, Is.Not.EqualTo(secondConfig.DevkitArmPath));
                    Assert.That(config.EmulatorPath, Is.Not.EqualTo(secondConfig.EmulatorPath));
                    if (!OperatingSystem.IsMacOS())
                    {
                        Assert.That(config.UseDocker, Is.Not.EqualTo(secondConfig.UseDocker));
                        Assert.That(config.DevkitArmDockerTag, Is.Not.EqualTo(secondConfig.DevkitArmDockerTag));
                    }
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(config.CurrentCultureName, Is.EqualTo(secondConfig.CurrentCultureName));
                    Assert.That(config.DisplayFont, Is.EqualTo(secondConfig.DisplayFont));
                    Assert.That(config.CheckForUpdates, Is.EqualTo(secondConfig.CheckForUpdates));
                    Assert.That(config.PreReleaseChannel, Is.EqualTo(secondConfig.PreReleaseChannel));
                    Assert.That(config.AutoReopenLastProject, Is.EqualTo(secondConfig.AutoReopenLastProject));
                    Assert.That(config.RememberProjectWorkspace, Is.EqualTo(secondConfig.RememberProjectWorkspace));
                    Assert.That(config.RemoveMissingProjects, Is.EqualTo(secondConfig.RemoveMissingProjects));
                    Assert.That(config.DevkitArmPath, Is.EqualTo(secondConfig.DevkitArmPath));
                    Assert.That(config.EmulatorPath, Is.EqualTo(secondConfig.EmulatorPath));
                    if (!OperatingSystem.IsMacOS())
                    {
                        Assert.That(config.UseDocker, Is.EqualTo(secondConfig.UseDocker));
                        Assert.That(config.DevkitArmDockerTag, Is.EqualTo(secondConfig.DevkitArmDockerTag));
                    }
                });
            }

            File.Delete(configPath);
        }
    }
}
