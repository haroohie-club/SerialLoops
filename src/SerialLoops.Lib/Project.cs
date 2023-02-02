using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
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
        public List<ItemDescription> Items { get; set; } = new();

        [JsonIgnore]
        public ArchiveFile<DataFile> Dat;
        [JsonIgnore]
        public ArchiveFile<GraphicsFile> Grp;
        [JsonIgnore]
        public ArchiveFile<EventFile> Evt;

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

        public void LoadArchives(ILogger log)
        {
            Dat = ArchiveFile<DataFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "dat.bin"), log);
            Grp = ArchiveFile<GraphicsFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "grp.bin"), log);
            Evt = ArchiveFile<EventFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "evt.bin"), log);

            BgTableFile bgTable = Dat.Files.First(f => f.Name == "BGTBLS").CastTo<BgTableFile>();
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
                    Items.Add(new BackgroundItem(name, i, entry, Evt, Grp));
                }
            }
            Items.AddRange(Evt.Files
                .Where(e => !new string[] { "CHESSS", "EVTTBLS", "TOPICS", "SCENARIOS", "TUTORIALS", "VOICEMAPS" }.Contains(e.Name))
                .Select(e => new ScriptItem(e)));
            QMapFile qmap = Dat.Files.First(f => f.Name == "QMAPS").CastTo<QMapFile>();
            Items.AddRange(Dat.Files
                .Where(d => qmap.QMaps.Select(q => q.Name.Replace(".", "")).Contains(d.Name))
                .Select(m => new MapItem(m.CastTo<MapFile>())));
            Items.AddRange(Dat.Files
                .Where(d => d.Name.StartsWith("SLG"))
                .Select(d => new PuzzleItem(d.CastTo<PuzzleFile>(), this)));
        }

        public ItemDescription FindItem(string name)
        {
            return Items.FirstOrDefault(i => i.Name == name);
        }

        public static Project OpenProject(string projFile, Config config, ILogger log)
        {
            log.Log($"Loading project from '{projFile}'...");
            Project project = JsonSerializer.Deserialize<Project>(File.ReadAllText(projFile));
            project.LoadArchives(log);
            return project;
        }
    }
}
