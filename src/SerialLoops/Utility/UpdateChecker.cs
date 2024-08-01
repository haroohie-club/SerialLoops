using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Assets;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.Utility
{
    internal class UpdateChecker(MainWindowViewModel mainWindowViewModel)
    {
        private const string USER_AGENT = "Serial-Loops-Updater";
        private const string ENDPOINT = "https://api.github.com";
        private const string ORG = "haroohie-club";
        private const string REPO = "SerialLoops";
        private const string GET_RELEASES = $"{ENDPOINT}/repos/{ORG}/{REPO}/releases";

        public bool UpdateOnClose { get; set; } = false;

        private readonly string _currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0.0";

        private MainWindowViewModel _mainWindowViewModel = mainWindowViewModel;
        private ILogger _logger => _mainWindowViewModel.Log;
        private bool _preReleaseChannel => _mainWindowViewModel.CurrentConfig.PreReleaseChannel;

        public async void Check()
        {
            (string version, string url, JsonArray assets, string changelog) = await GetLatestVersion(_currentVersion);
            if (_currentVersion.StartsWith(version)) // version might be something like 0.1.1, but current version will always be 4 digits
            {
                return;
            }

            _logger.Log($"An update for Serial Loops is available! ({version})");
            UpdateAvailableDialog updateDisplay = new();
            UpdateAvailableDialogViewModel updateDisplayViewModel = new();
            updateDisplayViewModel.Initialize(_mainWindowViewModel, updateDisplay, version, assets, url, changelog);
            updateDisplay.DataContext = updateDisplayViewModel;
            updateDisplay.ViewModel = updateDisplayViewModel;
            await updateDisplay.ShowDialog(_mainWindowViewModel.Window);
        }

        private async Task<(string, string, JsonArray, string)> GetLatestVersion(string currentVersion)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(USER_AGENT, currentVersion)
            );

            try
            {
                var response = await client.GetAsync(GET_RELEASES);
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonNode.Parse(content).AsArray();

                foreach (JsonNode release in json)
                {
                    if (bool.Parse(release["prerelease"].ToString()) == _preReleaseChannel)
                    {
                        return (release["tag_name"].ToString(), release["html_url"].ToString(), release["assets"].AsArray(), release["body"].ToString());
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogException(string.Format(Strings.Failed_to_check_for_updates___Endpoint___0__, GET_RELEASES), e);
            }

            return (currentVersion, "https://github.com/haroohie-club/SerialLoops/releases/latest", [], "N/A");
        }
    }
}
