using System.IO;

namespace SerialLoops.Tests.Shared
{
    public static class CommonHelpers
    {
        public static string CreateTestArtifactsFolder(string testName, string artifactsDir)
        {

            string testArtifactsFolder = Path.Combine(artifactsDir, testName.Replace(' ', '_').Replace(',', '_').Replace("\"", ""));
            if (Directory.Exists(testArtifactsFolder))
            {
                Directory.Delete(testArtifactsFolder, true);
            }
            Directory.CreateDirectory(testArtifactsFolder);
            return testArtifactsFolder;
        }
    }
}
