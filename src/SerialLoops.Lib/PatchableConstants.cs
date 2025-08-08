using System.IO;

namespace SerialLoops.Lib;

// This class exists to separate the values that get patched during the flatpak build to make that build
// go more smoothly
public static class PatchableConstants
{
    public static string LinuxDefaultLlvmDir => Path.Combine("/usr", "lib", "llvm");
    public const string FlatpakProcess = "flatpak";
    public static string[] FlatpakProcessBaseArgs => [];
    public const string FlatpakRunProcess = "";
    public static string[] FlatpakRunProcessBaseArgs => [];
    public const bool UseUpdater = true;
}
