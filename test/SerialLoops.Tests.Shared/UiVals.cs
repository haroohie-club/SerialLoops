namespace SerialLoops.UITests.Shared
{
    public class UiVals
    {
        public const string ROM_URI_ENV_VAR = "ROM_URI";
        public const string ROM_NAME = "HaruhiChokuretsu.nds";
        public const string APP_LOCATION_ENV_VAR = "APP_LOCATION";
        public const string PROJECT_NAME_ENV_VAR = "PROJECT_NAME";
        public const string ARTIFACTS_DIR_ENV_VAR = "BUILD_ARTIFACTSTAGINGDIRECTORY";
        public const string APPIUM_HOST_ENV_VAR = "APPIUM_HOST";

        public const string SKIP_UPDATE = "Skip Update";
        public const string NEW_PROJECT = "New Project";
        public const string OPEN_ROM = "Open ROM";
        public const string CREATE = "Create";
        public const string FILE = "File";
        public const string ABOUT_ELLIPSIS = "About…";
        public const string ABOUT = "About";
        public const string PREFERENCES = "Preferences…";
        public const string USE_DOCKER_FOR_ASM_HACKS = "Use Docker for ASM Hacks";
        public const string SAVE = "Save";
        public const string TOOLS = "Tools";
        public const string OK = "OK";
        public const string APPLY_HACKS = "Apply Hacks…";

        public string AppLoc { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string WinAppDriverLoc { get; set; } = string.Empty;
        public string RomLoc { get; set; } = string.Empty;
        public string ArtifactsDir { get; set; } = string.Empty;
    }
}
