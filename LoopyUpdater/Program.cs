using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace LoopyUpdater
{
    internal class Program
    {
        private const bool PRE_RELEASE_CHANNEL = true;
        private const string USER_AGENT = "Serial-Loops-Updater";
        private const string ENDPOINT = "https://api.github.com";
        private const string ORG = "haroohie-club";
        private const string REPO = "SerialLoops";
        private const string GET_RELEASES = $"{ENDPOINT}/repos/{ORG}/{REPO}/releases";

        private readonly string _currentVersion;
        private readonly string _executableFolder;
        
        private Program(string currentVersion, string executableFolder)
        {
            _currentVersion = currentVersion;
            _executableFolder = executableFolder;
        }
        
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid syntax.\n- Usage: LoopyUpdater <version>");
                return;
            }

            var executableDirectory = args.Length >= 2
                ? string.Join(" ", args[1..args.Length])
                : Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            new Program(args[0], executableDirectory).Run();
        }

        private void Run()
        {
            Console.WriteLine($"Checking for updates... (Current: {_currentVersion})");
            GetLatestVersion(_currentVersion).ContinueWith(async response =>
            {
                var latestVersion = await response;
                if (latestVersion.Equals(_currentVersion))
                {
                    Console.WriteLine($"Serial Loops is up-to-date! ({latestVersion})");
                    return;
                }

                Console.WriteLine($"An update for Serial Loops is available! ({latestVersion})");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    UpdateWindowsApp(latestVersion);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    UpdateMacApp(latestVersion);
                }
                else
                {
                    DisplayUpdateMessage(RuntimeInformation.OSDescription, latestVersion);
                }
            });
            Console.Read();
        }

        // todo
        private void UpdateWindowsApp(string latestVersion)
        {
            DisplayUpdateMessage("Windows", latestVersion);
        }

        // todo
        private void UpdateMacApp(string latestVersion)
        {
            DisplayUpdateMessage($"macOS ({RuntimeInformation.OSArchitecture.ToString()})", latestVersion);
        }

        private void DisplayUpdateMessage(string platform, string latestVersion)
        {
            Console.WriteLine("\n******************************\n" +
                              $"Please download the latest version of Serial Loops ({latestVersion}) for {platform} from\n" +
                              $"https://github.com/{ORG}/{REPO}/releases/{latestVersion}\n" +
                              "******************************\n");
            Console.Write("Press any key to exit.");
        }

        private async Task<string> GetLatestVersion(string currentVersion)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(USER_AGENT, currentVersion)
            );
            
            try
            {
                var response = await client.GetAsync(GET_RELEASES);
                var content = await response.Content.ReadAsStringAsync();
                var json = JArray.Parse(content);

                foreach (JToken release in json)
                {
                    if (release["prerelease"].ToObject<bool>() == PRE_RELEASE_CHANNEL)
                    {
                        return release["tag_name"].ToObject<string>();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to check for updates! (Endpoint: {GET_RELEASES}, Error: {e.Message})", e);
            }
            
            return currentVersion;
        }
    }
}