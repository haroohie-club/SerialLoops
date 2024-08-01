using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Avalonia.Headless.NUnit;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.Views;

namespace SerialLoops.Tests.Headless
{
    public class HomePanelTests
    {
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
                    ProjectName = Environment.GetEnvironmentVariable(UiVals.PROJECT_NAME_ENV_VAR) ?? "MacUITest",
                    RomLoc = romPath,
                    ArtifactsDir = Environment.GetEnvironmentVariable(UiVals.ARTIFACTS_DIR_ENV_VAR) ?? "artifacts",
                };
            }
        }

        [AvaloniaTest]
        public void ProjectCanBeCreatedClosedOpened()
        {
            MainWindowViewModel mainWindowViewModel = new();
            MainWindow window = new();
            mainWindowViewModel.Initialize(window);
            mainWindowViewModel.CurrentConfig.CheckForUpdates = false;
            

            window.Show();
            
        }
    }
}
