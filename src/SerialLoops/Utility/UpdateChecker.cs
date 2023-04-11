using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using System.Diagnostics;
using Eto.Forms;
using System.Runtime.InteropServices;

namespace SerialLoops.Utility
{
    internal class UpdateChecker
    {

        private const string USER_AGENT = "Serial-Loops-Updater";
        private const string ENDPOINT = "https://api.github.com";
        private const string ORG = "haroohie-club";
        private const string REPO = "SerialLoops";
        private const string GET_RELEASES = $"{ENDPOINT}/repos/{ORG}/{REPO}/releases";

        public bool UpdateOnClose { get; set; } = false;

        private readonly string _currentVersion;

        private MainForm _mainForm;
        private ILogger _logger => _mainForm.Log;
        private bool _preReleaseChannel => _mainForm.CurrentConfig.PreReleaseChannel;

        public UpdateChecker(MainForm mainForm)
        {
            _mainForm = mainForm;
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1";
        }

        public async void Check()
        {
            (string version, string url, JsonArray assets, string changelog) = await GetLatestVersion(_currentVersion);
            if (version.Equals(_currentVersion))
            {
                return;
            }

            _logger.Log($"An update for Serial Loops is available! ({version})");
            UpdateAvailableDialog updateDisplay = new(_mainForm, version, assets, url, changelog);
            updateDisplay.ShowModal(_mainForm);
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
                _logger.LogError($"Failed to check for updates! (Endpoint: {GET_RELEASES}, Error: {e.Message})");
            }

            return (currentVersion, "https://github.com/haroohie-club/SerialLoops/releases/latest", new(), "N/A");
        }

    }
}
