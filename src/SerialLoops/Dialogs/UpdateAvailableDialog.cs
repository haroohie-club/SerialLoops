using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Utility;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace SerialLoops.Dialogs
{
    public partial class UpdateAvailableDialog : Dialog
    {
        private readonly string _version;
        private readonly JsonArray _assets;
        private readonly string _url;
        private readonly string _changelog;

        private readonly MainForm _mainForm;
        private Config _config => _mainForm.CurrentConfig;
        private ILogger _log => _mainForm.Log;

        public UpdateAvailableDialog(MainForm mainForm, string version, JsonArray assets, string url, string changelog)
        {
            _version = version;
            _assets = assets;
            _url = url;
            _changelog = changelog;
            _mainForm = mainForm;

            InitializeComponent();   
        }

        private void InitializeComponent()
        {
            Title = string.Format(Application.Instance.Localize(this, "New Update Available: {0}"), _version);
            MinimumSize = new(600, 375);
            Resizable = false;

            Button updateButton = new() { Text = Application.Instance.Localize(this, "Update Now") };
            updateButton.Click += (s, e) => { PrepareUpdater(); };

            Button updateOnCloseButton = new() { Text = Application.Instance.Localize(this, "Update on Close") };
            updateOnCloseButton.Click += (s, e) => { PrepareUpdater(false); };

            Button downloadButton = new() { Text = Application.Instance.Localize(this, "Download from GitHub ") };
            downloadButton.Click += (s, e) => { Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true }); };

            Button skipButton = new() { Text = Application.Instance.Localize(this, "Skip Update") };
            skipButton.Click += (sender, args) => Close();

            CheckBox checkForUpdates = new() { Text = Application.Instance.Localize(this, "Check for Updates"), Checked = _config.CheckForUpdates };
            CheckBox preReleaseChannel = new() { Text = Application.Instance.Localize(this, "Pre-Release Channel"), Checked = _config.PreReleaseChannel };
            Closed += (sender, args) =>
            {
                _config.CheckForUpdates = checkForUpdates.Checked == true;
                _config.PreReleaseChannel = preReleaseChannel.Checked == true;
                _config.Save(_log);
            };

            Content = new TableLayout(
                new TableRow(GetUpdatePreview()),
                new TableRow(
                    new StackLayout
                    {
                        Padding = 10,
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            //updateButton,
                            //updateOnCloseButton,
                            downloadButton,
                            skipButton,
                            checkForUpdates,
                            preReleaseChannel
                        }
                    })
                );
        }

        private StackLayout GetUpdatePreview()
        {
            LinkButton urlLink = new() { Text = Application.Instance.Localize(this, "Download release from GitHub") };
            urlLink.Click += (sender, args) => { Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true }); };

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    ControlGenerator.GetTextHeader(string.Format(Application.Instance.Localize(this, "Serial Loops v{0}"), _version)),
                    ControlGenerator.GetControlWithIcon(Application.Instance.Localize(this, "A new update for Serial Loops is available!"), "Update", _log),
                    new Scrollable
                    {
                        Content = new RichTextArea
                        {
                            Text = _changelog,
                            ReadOnly = true,
                            Size = new(500, 250),
                            CaretIndex = 0,
                        }
                    },
                    urlLink
                }
            };
        }

        private void PrepareUpdater(bool shutDown = true)
        {
            Close();

            string assetUrl = SelectAssetUrl();
            if (assetUrl is null)
            {
                _log.LogError("Unable to select asset URL for download");
                return;
            }

            _mainForm.ShutdownUpdateUrl = assetUrl;
            if (shutDown)
            {
                _mainForm.Close();
            }
        }

        private string SelectAssetUrl()
        {
            string desiredContentType = "";
            string desiredFileStart = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                desiredContentType = "application/zip";
                desiredFileStart = "SerialLoops-windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                desiredContentType = "application/octet-stream";
                desiredFileStart = "SerialLoops-linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                desiredContentType = "application/x-apple-diskimage";
                desiredFileStart = "SerialLoops-macOS-x64";

                var architecture = RuntimeInformation.ProcessArchitecture;
                if (architecture == Architecture.Arm || architecture == Architecture.Arm64)
                {
                    desiredFileStart = "SerialLoops-macOS-arm";
                }
            }

            if (string.IsNullOrEmpty(desiredContentType) || string.IsNullOrEmpty(desiredFileStart))
            {
                return null;
            }


            foreach (var asset in _assets)
            {
                if (asset["name"].ToString().StartsWith(desiredFileStart) && asset["content_type"].ToString().Equals(desiredContentType))
                {
                    return asset["browser_download_url"].ToString();
                }
            }
            return null;
        }
    }
}
