using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
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
        public List<ItemDescription> Items { get; set; } = new();

        [JsonIgnore]
        public ArchiveFile<DataFile> Dat { get; set; }
        [JsonIgnore]
        public ArchiveFile<GraphicsFile> Grp { get; set; }
        [JsonIgnore]
        public ArchiveFile<EventFile> Evt { get; set; }

        [JsonIgnore]
        public FontReplacementDictionary FontReplacement { get; set; } = new();

        public Project()
        {
        }

        public Project(string name, string langCode, Config config, ILogger log)
        {
            Name = name;
            LangCode = langCode;
            MainDirectory = Path.Combine(config.ProjectsDirectory, name);
            log.Log("Creating project directories...");
            try
            {
                Directory.CreateDirectory(MainDirectory);
                File.WriteAllText(Path.Combine(MainDirectory, $"{Name}.{PROJECT_FORMAT}"), JsonSerializer.Serialize(this));
                Directory.CreateDirectory(BaseDirectory);
                Directory.CreateDirectory(IterativeDirectory);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred while attempting to create project directories.\n{exc.Message}\n\n{exc.StackTrace}");
            }
        }

        public void LoadArchives(ILogger log, IProgressTracker tracker)
        {
            tracker.Focus("dat.bin", 3);
            Dat = ArchiveFile<DataFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "dat.bin"), log);
            tracker.Finished++;

            tracker.CurrentlyLoading = "grp.bin";
            Grp = ArchiveFile<GraphicsFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "grp.bin"), log);
            tracker.Finished++;

            tracker.CurrentlyLoading = "evt.bin";
            Evt = ArchiveFile<EventFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "evt.bin"), log);
            tracker.Finished++;

            tracker.Focus("Font Replacement Dictionary", 1);
            if (IO.TryReadStringFile(Path.Combine(BaseDirectory, "assets", "misc", "charset.json"), out string json, log))
            {
                FontReplacement.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(json));
            }
            else
            {
                log.LogError("Failed to load font replacement dictionary.");
            }
            tracker.Finished++;

            tracker.Focus("Extras", 1);
            ExtraFile extras = Dat.Files.First(f => f.Name == "EXTRAS").CastTo<ExtraFile>();
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
                    Items.Add(new BackgroundItem(name, i, entry, Evt, Grp, extras));
                }
                tracker.Finished++;
            }

            string[] bgmFiles = Directory.GetFiles(Path.Combine(IterativeDirectory, "original", "bgm")).OrderBy(s => s).ToArray();
            tracker.Focus("BGM Tracks", bgmFiles.Length);
            for (int i = 0; i < bgmFiles.Length; i++)
            {
                Items.Add(new BackgroundMusicItem(bgmFiles[i], i, extras, this));
                tracker.Finished++;
            }

            string[] voiceFiles = Directory.GetFiles(Path.Combine(IterativeDirectory, "original", "vce")).OrderBy(s => s).ToArray();
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
                .Select(e => new ScriptItem(e)));
            tracker.Finished++;

            tracker.Focus("Maps", 1);
            QMapFile qmap = Dat.Files.First(f => f.Name == "QMAPS").CastTo<QMapFile>();
            Items.AddRange(Dat.Files
                .Where(d => qmap.QMaps.Select(q => q.Name.Replace(".", "")).Contains(d.Name))
                .Select(m => new MapItem(m.CastTo<MapFile>(), qmap.QMaps.FindIndex(q => q.Name.Replace(".", "") == m.Name))));
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
        }

        public ItemDescription FindItem(string name)
        {
            return Items.FirstOrDefault(i => i.Name == name.Split(" - ")[0]);
        }

        public static Project OpenProject(string projFile, Config config, ILogger log, IProgressTracker tracker)
        {
            log.Log($"Loading project from '{projFile}'...");
            tracker.Focus($"{Path.GetFileNameWithoutExtension(projFile)} Project Data", 1);
            Project project = JsonSerializer.Deserialize<Project>(File.ReadAllText(projFile));
            tracker.Finished++;
            project.LoadArchives(log, tracker);
            return project;
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
