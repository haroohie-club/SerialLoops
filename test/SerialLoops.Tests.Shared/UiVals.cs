// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SerialLoops.Tests.Shared
{
    public class UiVals
    {
        public const string ROM_URI_ENV_VAR = "ROM_URI";
        public const string ROM_NAME = "HaruhiChokuretsu.nds";
        public const string PROJECT_NAME_ENV_VAR = "PROJECT_NAME";
        public const string ARTIFACTS_DIR_ENV_VAR = "BUILD_ARTIFACTSTAGINGDIRECTORY";

        public string RomLoc { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ArtifactsDir { get; set; } = string.Empty;
    }
}
