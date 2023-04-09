using HaroohieClub.NitroPacker.Core;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using HaruhiChokuretsuLib.Util.Exceptions;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialLoops.Lib
{
    public class Project
    {
        public const string PROJECT_FORMAT = "slproj";

        public string Name { get; set; }
        public string LangCode { get; set; }
        public string MainDirectory { get; set; }
        [JsonIgnore]
        public string BaseDirectory => Path.Combine(MainDirectory, "base");
        [JsonIgnore]
        public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");
        [JsonIgnore]
        public string ProjectFile => Path.Combine(MainDirectory, $"{Name}.{PROJECT_FORMAT}");
        [JsonIgnore]
        public Config Config { get; private set; }
        [JsonIgnore]
        public ProjectSettings Settings { get; set; }
        [JsonIgnore]
        public List<ItemDescription> Items { get; set; } = new();

        [JsonIgnore]
        public ArchiveFile<DataFile> Dat { get; set; }
        [JsonIgnore]
        public ArchiveFile<GraphicsFile> Grp { get; set; }
        [JsonIgnore]
        public ArchiveFile<EventFile> Evt { get; set; }

        [JsonIgnore]
        public FontReplacementDictionary FontReplacement { get; set; } = new();
        [JsonIgnore]
        public FontFile FontMap { get; set; } = new();
        [JsonIgnore]
        public SKBitmap SpeakerBitmap { get; set; }
        [JsonIgnore]
        public SKBitmap DialogueBitmap { get; set; }
        [JsonIgnore]
        public SKBitmap FontBitmap { get; set; }

        [JsonIgnore]
        public ExtraFile Extra { get; set; }

        public Project()
        {
        }

        public Project(string name, string langCode, Config config, ILogger log)
        {
            Name = name;
            LangCode = langCode;
            MainDirectory = Path.Combine(config.ProjectsDirectory, name);
            Config = config;
            log.Log("Creating project directories...");
            try
            {
                Directory.CreateDirectory(MainDirectory);
                File.WriteAllText(Path.Combine(MainDirectory, $"{Name}.{PROJECT_FORMAT}"), JsonSerializer.Serialize(this));
                Directory.CreateDirectory(BaseDirectory);
                Directory.CreateDirectory(IterativeDirectory);
                Directory.CreateDirectory(Path.Combine(MainDirectory, "font"));
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "charset.json"), Path.Combine(MainDirectory, "font", "charset.json"));
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred while attempting to create project directories.\n{exc.Message}\n\n{exc.StackTrace}");
            }
        }

        public enum LoadProjectState
        {
            SUCCESS,
            LOOSELEAF_FILES,
            CORRUPTED_FILE,
            NOT_FOUND,
            FAILED,
        }

        public struct LoadProjectResult
        {
            public LoadProjectState State { get; set; }
            public string BadArchive { get; set; }
            public int BadFileIndex { get; set; }

            public LoadProjectResult(LoadProjectState state, string badArchive, int badFileIndex)
            {
                State = state;
                BadArchive = badArchive;
                BadFileIndex = badFileIndex;
            }
            public LoadProjectResult(LoadProjectState state)
            {
                State = state;
                BadArchive = string.Empty;
                BadFileIndex= -1;
            }
        }
        
        public LoadProjectResult Load(Config config, ILogger log, IProgressTracker tracker)
        {
            Config = config;
            LoadProjectSettings(log, tracker);
            ClearOrCreateCaches(config.CachesDirectory, log);
            if (Directory.GetFiles(Path.Combine(IterativeDirectory, "assets"), "*", SearchOption.AllDirectories).Length > 0)
            {
                return new(LoadProjectState.LOOSELEAF_FILES);
            }
            return LoadArchives(log, tracker);
        }

        public void LoadProjectSettings(ILogger log, IProgressTracker tracker)
        {
            tracker.Focus("Project Settings", 1);
            byte[] projectFile = File.ReadAllBytes(Path.Combine(IterativeDirectory, "rom", $"{Name}.xml"));
            Settings = new(NdsProjectFile.FromByteArray<NdsProjectFile>(projectFile), log);
            tracker.Finished++;
        }
        
        public LoadProjectResult LoadArchives(ILogger log, IProgressTracker tracker)
        {
            tracker.Focus("dat.bin", 3);
            try
            {
                Dat = ArchiveFile<DataFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "dat.bin"), log, false);
            }
            catch (ArchiveLoadException ex)
            {
                if (Directory.GetFiles(Path.Combine(BaseDirectory, "assets", "data")).Any(f => Path.GetFileNameWithoutExtension(f) == $"{ex.Index:X3}"))
                {
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in dat.bin was detected to be corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "dat.bin", ex.Index);
                }
                else
                {
                    // If it's not a file they've modified, then they're using a bad base ROM
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in dat.bin was detected to be corrupted. " +
                        $"Please use a different base ROM as this one is corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "dat.bin", -1);
                }
            }
            tracker.Finished++;

            tracker.CurrentlyLoading = "grp.bin";
            try
            {
                Grp = ArchiveFile<GraphicsFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "grp.bin"), log);
            }
            catch (ArchiveLoadException ex)
            {
                if (Directory.GetFiles(Path.Combine(BaseDirectory, "assets", "graphics")).Any(f => Path.GetFileNameWithoutExtension(f) == $"{ex.Index:X3}"))
                {
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in grp.bin was detected to be corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "grp.bin", ex.Index);
                }
                else
                {
                    // If it's not a file they've modified, then they're using a bad base ROM
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in grp.bin was detected to be corrupted. " +
                        $"Please use a different base ROM as this one is corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "grp.bin", -1);
                }
            }
            tracker.Finished++;

            tracker.CurrentlyLoading = "evt.bin";
            try
            {
                Evt = ArchiveFile<EventFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "evt.bin"), log);
            }
            catch (ArchiveLoadException ex)
            {
                if (Directory.GetFiles(Path.Combine(BaseDirectory, "assets", "events")).Any(f => Path.GetFileNameWithoutExtension(f) == $"{ex.Index:X3}"))
                {
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in evt.bin was detected to be corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "evt.bin", ex.Index);
                }
                else
                {
                    // If it's not a file they've modified, then they're using a bad base ROM
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in evt.bin was detected to be corrupted. " +
                        $"Please use a different base ROM as this one is corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "evt.bin", -1);
                }
            }
            tracker.Finished++;

            tracker.Focus("Font", 5);
            if (IO.TryReadStringFile(Path.Combine(MainDirectory, "font", "charset.json"), out string json, log))
            {
                FontReplacement.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(json));
            }
            else
            {
                log.LogError("Failed to load font replacement dictionary.");
            }
            tracker.Finished++;
            FontMap = Dat.Files.First(f => f.Name == "FONTS").CastTo<FontFile>();
            tracker.Finished++;
            SpeakerBitmap = Grp.Files.First(f => f.Name == "SYS_CMN_B12DNX").GetImage(transparentIndex: 0);
            tracker.Finished++;
            DialogueBitmap = Grp.Files.First(f => f.Name == "SYS_CMN_B02DNX").GetImage(transparentIndex: 0);
            tracker.Finished++;
            GraphicsFile fontFile = Grp.Files.First(f => f.Name == "ZENFONTBNF");
            fontFile.InitializeFontFile();
            FontBitmap = Grp.Files.First(f => f.Name == "ZENFONTBNF").GetImage(transparentIndex: 0);
            tracker.Finished++;

            tracker.Focus("Extras", 1);
            Extra = Dat.Files.First(f => f.Name == "EXTRAS").CastTo<ExtraFile>();
            tracker.Finished++;

            BgTableFile bgTable = Dat.Files.First(f => f.Name == "BGTBLS").CastTo<BgTableFile>();
            tracker.Focus("Backgrounds", bgTable.BgTableEntries.Count);
            for (int i = 0; i < bgTable.BgTableEntries.Count; i++)
            {
                BgTableEntry entry = bgTable.BgTableEntries[i];
                if (entry.BgIndex1 > 0)
                {
                    GraphicsFile nameGraphic = Grp.Files.First(g => g.Index == entry.BgIndex1);
                    string name = $"BG_{nameGraphic.Name[0..(nameGraphic.Name.LastIndexOf('_'))]}";
                    string bgNameBackup = name;
                    for (int j = 1; Items.Select(i => i.Name).Contains(name); j++)
                    {
                        name = $"{bgNameBackup}{j:D2}";
                    }
                    Items.Add(new BackgroundItem(name, i, entry, this));
                }
                tracker.Finished++;
            }

            string[] bgmFiles = Directory.GetFiles(Path.Combine(IterativeDirectory, "rom", "data", "bgm")).OrderBy(s => s).ToArray();
            tracker.Focus("BGM Tracks", bgmFiles.Length);
            for (int i = 0; i < bgmFiles.Length; i++)
            {
                Items.Add(new BackgroundMusicItem(bgmFiles[i], i, this));
                tracker.Finished++;
            }

            string[] voiceFiles = Directory.GetFiles(Path.Combine(IterativeDirectory, "rom", "data", "vce")).OrderBy(s => s).ToArray();
            tracker.Focus("Voiced Lines", voiceFiles.Length);
            for (int i = 0; i < voiceFiles.Length; i++)
            {
                Items.Add(new VoicedLineItem(voiceFiles[i], i + 1, this));
                tracker.Finished++;
            }

            tracker.Focus("Character Sprites", 1);
            CharacterDataFile chrdata = Dat.Files.First(d => d.Name == "CHRDATAS").CastTo<CharacterDataFile>();
            Items.AddRange(chrdata.Sprites.Where(s => (int)s.Character > 0).Select(s => new CharacterSpriteItem(s, chrdata, this)));
            tracker.Finished++;

            tracker.Focus("Chibis", 1);
            Items.AddRange(Dat.Files.First(d => d.Name == "CHIBIS").CastTo<ChibiFile>()
                .Chibis.Select(c => new ChibiItem(c, this)));
            tracker.Finished++;

            tracker.Focus("Dialogue Configs", 1);
            Items.AddRange(Dat.Files.First(d => d.Name == "MESSINFOS").CastTo<MessageInfoFile>()
                .MessageInfos.Where(m => (int)m.Character > 0).Select(m => new DialogueConfigItem(m)));
            tracker.Finished++;

            tracker.Focus("Event Files", 1);
            Items.AddRange(Evt.Files
                .Where(e => !new string[] { "CHESSS", "EVTTBLS", "TOPICS", "SCENARIOS", "TUTORIALS", "VOICEMAPS" }.Contains(e.Name))
                .Select(e => new ScriptItem(e, log)));
            tracker.Finished++;

            tracker.Focus("Maps", 1);
            QMapFile qmap = Dat.Files.First(f => f.Name == "QMAPS").CastTo<QMapFile>();
            Items.AddRange(Dat.Files
                .Where(d => qmap.QMaps.Select(q => q.Name.Replace(".", "")).Contains(d.Name))
                .Select(m => new MapItem(m.CastTo<MapFile>(), qmap.QMaps.FindIndex(q => q.Name.Replace(".", "") == m.Name), this)));
            tracker.Finished++;

            PlaceFile placeFile = Dat.Files.First(f => f.Name == "PLACES").CastTo<PlaceFile>();
            tracker.Focus("Places", placeFile.PlaceGraphicIndices.Count);
            for (int i = 0; i < placeFile.PlaceGraphicIndices.Count; i++)
            {
                GraphicsFile placeGrp = Grp.Files.First(g => g.Index == placeFile.PlaceGraphicIndices[i]);
                Items.Add(new PlaceItem(i, placeGrp, this));
            }
            tracker.Finished++;

            tracker.Focus("Puzzles", 1);
            Items.AddRange(Dat.Files
                .Where(d => d.Name.StartsWith("SLG"))
                .Select(d => new PuzzleItem(d.CastTo<PuzzleFile>(), this)));
            tracker.Finished++;

            tracker.Focus("Topics", 2);
            Evt.Files.First(f => f.Name == "TOPICS").InitializeTopicFile();
            tracker.Finished++;
            Items.AddRange(Evt.Files.First(f => f.Name == "TOPICS").TopicStructs.Select(t => new TopicItem(t, this)));
            tracker.Finished++;

            // Scenario item must be created after script and puzzle items are constructed
            tracker.Focus("Scenario", 1);
            EventFile scenarioFile = Evt.Files.First(f => f.Name == "SCENARIOS");
            scenarioFile.InitializeScenarioFile();
            Items.Add(new ScenarioItem(scenarioFile.Scenario, this));
            tracker.Finished++;

            tracker.Focus("Group Selections", scenarioFile.Scenario.Selects.Count);
            for (int i = 0; i < scenarioFile.Scenario.Selects.Count; i++)
            {
                Items.Add(new GroupSelectionItem(scenarioFile.Scenario.Selects[i], i, this));
                tracker.Finished++;
            }

            return new(LoadProjectState.SUCCESS);
        }

        public void MigrateProject(string newRom, ILogger log, IProgressTracker tracker)
        {
            log.Log($"Attempting to migrate to new ROM {newRom}");

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            NdsProjectFile.Create("temp", newRom, tempDir);
            IO.CopyFiles(Path.Combine(tempDir, "data"), Path.Combine(BaseDirectory, "original", "archives"), "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "data", "bgm"), Path.Combine(BaseDirectory, "original", "bgm"), "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "data", "vce"), Path.Combine(BaseDirectory, "original", "vce"), "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "overlay"), Path.Combine(BaseDirectory, "original", "overlay"), "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "data", "movie"), Path.Combine(BaseDirectory, "rom", "data", "movie"), "*.mods");

            Directory.Delete(tempDir, true);
        }

        public static void ClearOrCreateCaches(string cachesDirectory, ILogger log)
        {
            if (Directory.Exists(cachesDirectory))
            {
                Directory.Delete(cachesDirectory, true);
            }

            log.Log("Creating cache directory...");
            Directory.CreateDirectory(cachesDirectory);

            string bgmCache = Path.Combine(cachesDirectory, "bgm");
            log.Log("Creating BGM cache...");
            Directory.CreateDirectory(bgmCache);

            string vceCache = Path.Combine(cachesDirectory, "vce");
            log.Log("Creating voice file cache...");
            Directory.CreateDirectory(vceCache);
        }

        public ItemDescription FindItem(string name)
        {
            return Items.FirstOrDefault(i => i.Name == name.Split(" - ")[0]);
        }

        public static (Project Project, LoadProjectResult Result) OpenProject(string projFile, Config config, ILogger log, IProgressTracker tracker)
        {
            log.Log($"Loading project from '{projFile}'...");
            if (!File.Exists(projFile))
            {
                log.LogError($"Project file {projFile} not found -- has it been deleted?");
                return (null, new(LoadProjectState.NOT_FOUND));
            }
            try
            {
                tracker.Focus($"{Path.GetFileNameWithoutExtension(projFile)} Project Data", 1);
                Project project = JsonSerializer.Deserialize<Project>(File.ReadAllText(projFile));
                tracker.Finished++;
                LoadProjectResult result = project.Load(config, log, tracker);
                if (result.State == LoadProjectState.LOOSELEAF_FILES)
                {
                    log.LogWarning("Found looseleaf files in iterative directory; prompting user for build before loading archives...");
                }
                else if (result.State == LoadProjectState.CORRUPTED_FILE)
                {
                    log.LogWarning("Found corrupted file in archive; prompting user for action before continuing...");
                }
                return (project, result);
            }
            catch (Exception exc)
            {
                log.LogError($"Error while loading project: {exc.Message}\n\n{exc.StackTrace}");
                return (null, new(LoadProjectState.FAILED));
            }
        }

        public List<ItemDescription> GetSearchResults(string searchTerm, bool titlesOnly = true)
        {
            if (titlesOnly)
            {
                return Items.Where(item =>
                    item.Name.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                return Items.Where(item => 
                    item.Name.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase) || 
                    item.DisplayName.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(item.SearchableText) &&
                    item.SearchableText.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();
            }
        }

    }
}
