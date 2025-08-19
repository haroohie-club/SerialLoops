using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Assets;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.Utility;

internal class UpdateChecker(MainWindowViewModel mainWindowViewModel)
{
    private const string USER_AGENT = "Serial-Loops-Updater";
    private const string ENDPOINT = "https://api.github.com";
    private const string ORG = "haroohie-club";
    private const string REPO = "SerialLoops";
    private const string GET_RELEASES = $"{ENDPOINT}/repos/{ORG}/{REPO}/releases";

    private readonly string _currentVersion = Shared.GetVersion();

    private MainWindowViewModel _mainWindowViewModel = mainWindowViewModel;
    private ILogger _logger => _mainWindowViewModel.Log;
    private bool _preReleaseChannel => _mainWindowViewModel.CurrentConfig.PreReleaseChannel;

    public async Task Check()
    {
        (string version, string url, _, string changelog) = await GetLatestVersion(_currentVersion);
        if (_currentVersion.StartsWith(version)) // version might be something like 0.1.1, but current version will always be 4 digits
        {
            return;
        }

        _logger.Log($"An update for Serial Loops is available! ({version})");
        await new UpdateAvailableDialog
        {
            DataContext = new UpdateAvailableDialogViewModel(_mainWindowViewModel, version, url, changelog),
        }.ShowDialog(_mainWindowViewModel.Window);
    }

    private async Task<(string, string, JsonArray, string)> GetLatestVersion(string currentVersion)
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.Add(
            new(USER_AGENT, currentVersion)
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
            _logger.LogException(string.Format(Strings.ErrorFailedCheckingForUpdates, GET_RELEASES), e);
        }

        return (currentVersion, "https://github.com/haroohie-club/SerialLoops/releases/latest", [], "N/A");
    }
}
