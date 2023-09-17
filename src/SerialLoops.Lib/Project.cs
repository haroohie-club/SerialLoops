using HaroohieClub.NitroPacker.Core;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using HaruhiChokuretsuLib.Util.Exceptions;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialLoops.Lib
{
    public class Project
    {
        public const string PROJECT_FORMAT = "slproj";
        public static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { Converters = { new SKColorJsonConverter() } };

        public string Name { get; set; }
        public string LangCode { get; set; }
        public string MainDirectory { get; set; }
        public Dictionary<string, string> ItemNames { get; set; }
        public Dictionary<int, NameplateProperties> Characters { get; set; }

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
        public SoundArchive Snd { get; set; }

        [JsonIgnore]
        public FontReplacementDictionary FontReplacement { get; set; } = new();
        [JsonIgnore]
        public FontFile FontMap { get; set; } = new();
        [JsonIgnore]
        public SKBitmap SpeakerBitmap { get; set; }
        [JsonIgnore]
        public SKBitmap NameplateBitmap { get; set; }
        [JsonIgnore]
        public SKBitmap DialogueBitmap { get; set; }
        [JsonIgnore]
        public SKBitmap FontBitmap { get; set; }

        [JsonIgnore]
        public ExtraFile Extra { get; set; }
        [JsonIgnore]
        public ScenarioStruct Scenario { get; set; }
        [JsonIgnore]
        public SoundDSFile SoundDS { get; set; }
        [JsonIgnore]
        public EventFile TopicFile { get; set; }
        [JsonIgnore]
        public EventFile TutorialFile { get; set; }
        [JsonIgnore]
        public MessageFile UiText { get; set; }
        [JsonIgnore]
        public MessageInfoFile MessInfo { get; set; }
        [JsonIgnore]
        public VoiceMapFile VoiceMap { get; set; }

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
                Save(log);
                Directory.CreateDirectory(BaseDirectory);
                Directory.CreateDirectory(IterativeDirectory);
                Directory.CreateDirectory(Path.Combine(MainDirectory, "font"));
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "charset.json"), Path.Combine(MainDirectory, "font", "charset.json"));
            }
            catch (Exception ex)
            {
                log.LogException("Exception occurred while attempting to create project directories.", ex);
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
                BadFileIndex = -1;
            }
        }

        public LoadProjectResult Load(Config config, ILogger log, IProgressTracker tracker)
        {
            Config = config;
            LoadProjectSettings(log, tracker);
            ClearOrCreateCaches(config.CachesDirectory, log);
            string makefile = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_main"));
            if (!makefile.Equals(File.ReadAllText(Path.Combine(BaseDirectory, "src", "Makefile"))))
            {
                IO.CopyFileToDirectories(this, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_main"), Path.Combine("src", "Makefile"), log);
                IO.CopyFileToDirectories(this, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_overlay"), Path.Combine("src", "overlays", "Makefile"), log);
            }
            if (!string.IsNullOrEmpty(config.DevkitArmPath))
            {
                string devkitARMVersionish = Path.GetFileNameWithoutExtension(Directory.GetDirectories(Path.Combine(config.DevkitArmPath, "lib", "gcc", "arm-none-eabi"))[0]);

                log.Log($"DevkitARM version detected as {devkitARMVersionish}");
                if (!makefile.Contains(devkitARMVersionish))
                {
                    log.LogError($"DevkitARM is most likely out of date! (Or, possibly, we are!) If you haven't installed devkitARM recently, consider upgrading.");
                }
            }
            if (Directory.GetFiles(Path.Combine(IterativeDirectory, "assets"), "*", SearchOption.AllDirectories).Length > 0)
            {
                return new(LoadProjectState.LOOSELEAF_FILES);
            }
            return LoadArchives(log, tracker);
        }

        public void LoadProjectSettings(ILogger log, IProgressTracker tracker)
        {
            tracker.Focus("Project Settings", 1);
            string projPath = Path.Combine(IterativeDirectory, "rom", $"{Name}.xml");
            try
            {
                byte[] projectFile = File.ReadAllBytes(projPath);
                Settings = new(NdsProjectFile.FromByteArray<NdsProjectFile>(projectFile), log);
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load project from {projPath}", ex);
            }
            tracker.Finished++;
        }

        public LoadProjectResult LoadArchives(ILogger log, IProgressTracker tracker)
        {
            tracker.Focus("dat.bin", 4);
            try
            {
                Dat = ArchiveFile<DataFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "dat.bin"), log, false);
            }
            catch (ArchiveLoadException ex)
            {
                if (Directory.GetFiles(Path.Combine(BaseDirectory, "assets", "data")).Any(f => Path.GetFileNameWithoutExtension(f) == $"{ex.Index:X3}"))
                {
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in dat.bin was detected as corrupt.");
                    return new(LoadProjectState.CORRUPTED_FILE, "dat.bin", ex.Index);
                }
                else
                {
                    // If it's not a file they've modified, then they're using a bad base ROM
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in dat.bin was detected as corrupt. " +
                        $"Please use a different base ROM as this one is corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "dat.bin", -1);
                }
            }
            catch (Exception ex)
            {
                log.LogException("Error occurred while loading dat.bin", ex);
                return new(LoadProjectState.FAILED);
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
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in grp.bin was detected as corrupt.");
                    return new(LoadProjectState.CORRUPTED_FILE, "grp.bin", ex.Index);
                }
                else
                {
                    // If it's not a file they've modified, then they're using a bad base ROM
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in grp.bin was detected as corrupt. " +
                        $"Please use a different base ROM as this one is corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "grp.bin", -1);
                }
            }
            catch (Exception ex)
            {
                log.LogException("Error occurred while loading grp.bin", ex);
                return new(LoadProjectState.FAILED);
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
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in evt.bin was detected as corrupt.");
                    return new(LoadProjectState.CORRUPTED_FILE, "evt.bin", ex.Index);
                }
                else
                {
                    // If it's not a file they've modified, then they're using a bad base ROM
                    log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in evt.bin was detected as corrupt. " +
                        $"Please use a different base ROM as this one is corrupted.");
                    return new(LoadProjectState.CORRUPTED_FILE, "evt.bin", -1);
                }
            }
            catch (Exception ex)
            {
                log.LogException("Error occurred while loading evt.bin", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;

            tracker.CurrentlyLoading = "snd.bin";
            try
            {
                Snd = new(Path.Combine(IterativeDirectory, "original", "archives", "snd.bin"));
            }
            catch (Exception ex)
            {
                log.LogException("Error occurred while loading snd.bin", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;

            string charactersFile = LangCode switch
            {
                "ja" => "DefaultCharacters.ja.json",
                _ => "DefaultCharacters.en.json"
            };
            try
            {
                Characters ??= JsonSerializer.Deserialize<Dictionary<int, NameplateProperties>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", charactersFile)), SERIALIZER_OPTIONS);
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load DefaultCharacters file", ex);
                return new(LoadProjectState.FAILED);
            }

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
            try
            {
                FontMap = Dat.Files.First(f => f.Name == "FONTS").CastTo<FontFile>();
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load font map", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;
            try
            {
                SpeakerBitmap = Grp.Files.First(f => f.Name == "SYS_CMN_B12DNX").GetImage(transparentIndex: 0);
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load speaker bitmap", ex);
                return new(LoadProjectState.FAILED);
            }
            try
            {
                NameplateBitmap = Grp.Files.First(f => f.Name == "SYS_CMN_B12DNX").GetImage();
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load nameplate bitmap", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;
            try
            {
                DialogueBitmap = Grp.Files.First(f => f.Name == "SYS_CMN_B02DNX").GetImage(transparentIndex: 0);
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load dialogue bitmap", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;
            try
            {
                GraphicsFile fontFile = Grp.Files.First(f => f.Name == "ZENFONTBNF");
                fontFile.InitializeFontFile();
                FontBitmap = Grp.Files.First(f => f.Name == "ZENFONTBNF").GetImage(transparentIndex: 0);
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load font bitmap", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;

            tracker.Focus("Static Files", 4);
            try
            {
                Extra = Dat.Files.First(f => f.Name == "EXTRAS").CastTo<ExtraFile>();
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load extra file", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;
            try
            {
                EventFile scenario = Evt.Files.First(f => f.Name == "SCENARIOS");
                scenario.InitializeScenarioFile();
                Scenario = scenario.Scenario;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load scenario file", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;
            try
            {
                MessInfo = Dat.Files.First(f => f.Name == "MESSINFOS").CastTo<MessageInfoFile>();
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load message info file", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;
            try
            {
                UiText = Dat.Files.First(f => f.Name == "MESSS").CastTo<MessageFile>();
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load UI text file", ex);
                return new(LoadProjectState.FAILED);
            }
            tracker.Finished++;

            try
            {
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
            }
            catch (Exception ex)
            {
                log.LogException("Failed to background items", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                SoundDS = Dat.Files.First(s => s.Name == "SND_DSS").CastTo<SoundDSFile>();
            }
            catch (Exception ex)
            {
                log.LogException("Failed to load DS sound file.", ex);
            }
            try
            {
                if (VoiceMapIsV06OrHigher())
                {
                    VoiceMap = Evt.Files.First(v => v.Name == "VOICEMAPS").CastTo<VoiceMapFile>();
                }
            }
            catch (Exception ex)
            {
                log.LogException("Failed to load voice map", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                string[] bgmFiles = SoundDS.BgmSection.Where(bgm => bgm is not null).Select(bgm => Path.Combine(IterativeDirectory, "rom", "data", bgm)).ToArray(); /*Directory.GetFiles(Path.Combine(IterativeDirectory, "rom", "data", "bgm")).OrderBy(s => s).ToArray();*/
                tracker.Focus("BGM Tracks", bgmFiles.Length);
                for (int i = 0; i < bgmFiles.Length; i++)
                {
                    Items.Add(new BackgroundMusicItem(bgmFiles[i], i, this));
                    tracker.Finished++;
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load BGM tracks", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                string[] voiceFiles = SoundDS.VoiceSection.Where(vce => vce is not null).Select(vce => Path.Combine(IterativeDirectory, "rom", "data", vce)).ToArray(); /*Directory.GetFiles(Path.Combine(IterativeDirectory, "rom", "data", "vce")).OrderBy(s => s).ToArray();*/
                tracker.Focus("Voiced Lines", voiceFiles.Length);
                for (int i = 0; i < voiceFiles.Length; i++)
                {
                    Items.Add(new VoicedLineItem(voiceFiles[i], i + 1, this));
                    tracker.Finished++;
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load voiced lines", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                tracker.Focus("Sound Effects", SoundDS.SfxSection.Count);
                for (short i = 0; i < SoundDS.SfxSection.Count; i++)
                {
                    if (SoundDS.SfxSection[i].Index < Snd.SequenceArchives[SoundDS.SfxSection[i].SequenceArchive].File.Sequences.Count)
                    {
                        string name = Snd.SequenceArchives[SoundDS.SfxSection[i].SequenceArchive].File.Sequences[SoundDS.SfxSection[i].Index].Name;
                        if (!name.Equals("SE_DUMMY"))
                        {
                            Items.Add(new SfxItem(SoundDS.SfxSection[i], name, i, this));
                        }
                    }
                    tracker.Finished++;
                }
            }
            catch (Exception ex)
            {
                log.LogException("Failed to load sound effects", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                tracker.Focus("Character Sprites", 1);
                CharacterDataFile chrdata = Dat.Files.First(d => d.Name == "CHRDATAS").CastTo<CharacterDataFile>();
                Items.AddRange(chrdata.Sprites.Where(s => (int)s.Character > 0).Select(s => new CharacterSpriteItem(s, chrdata, this)));
                tracker.Finished++;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load character sprites", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                ChibiFile chibiFile = Dat.Files.First(d => d.Name == "CHIBIS").CastTo<ChibiFile>();
                tracker.Focus("Chibis", chibiFile.Chibis.Count);
                foreach (Chibi chibi in chibiFile.Chibis)
                {
                    Items.Add(new ChibiItem(chibi, this));
                    tracker.Finished++;
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load chibis", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                tracker.Focus("Characters", 1);
                Items.AddRange(Dat.Files.First(d => d.Name == "MESSINFOS").CastTo<MessageInfoFile>()
                    .MessageInfos.Where(m => (int)m.Character > 0).Select(m => new CharacterItem(m, Characters[(int)m.Character], this)));
                tracker.Finished++;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load characters", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                tracker.Focus("Scripts", 1);
                Items.AddRange(Evt.Files
                    .Where(e => !new string[] { "CHESSS", "EVTTBLS", "TOPICS", "SCENARIOS", "TUTORIALS", "VOICEMAPS" }.Contains(e.Name))
                    .Select(e => new ScriptItem(e, log)));
                tracker.Finished++;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load scripts", ex);
                return new(LoadProjectState.FAILED);
            }


            try
            {
                TutorialFile = Evt.Files.First(t => t.Name == "TUTORIALS");
                TutorialFile.InitializeTutorialFile();
            }
            catch (Exception ex)
            {
                log.LogException("Failed to load tutorials", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                tracker.Focus("Maps", 1);
                QMapFile qmap = Dat.Files.First(f => f.Name == "QMAPS").CastTo<QMapFile>();
                Items.AddRange(Dat.Files
                    .Where(d => qmap.QMaps.Select(q => q.Name.Replace(".", "")).Contains(d.Name))
                    .Select(m => new MapItem(m.CastTo<MapFile>(), qmap.QMaps.FindIndex(q => q.Name.Replace(".", "") == m.Name), this)));
                tracker.Finished++;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load maps", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                PlaceFile placeFile = Dat.Files.First(f => f.Name == "PLACES").CastTo<PlaceFile>();
                tracker.Focus("Places", placeFile.PlaceGraphicIndices.Count);
                for (int i = 0; i < placeFile.PlaceGraphicIndices.Count; i++)
                {
                    GraphicsFile placeGrp = Grp.Files.First(g => g.Index == placeFile.PlaceGraphicIndices[i]);
                    Items.Add(new PlaceItem(i, placeGrp, this));
                }
                tracker.Finished++;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load place items", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                tracker.Focus("Puzzles", 1);
                Items.AddRange(Dat.Files
                    .Where(d => d.Name.StartsWith("SLG"))
                    .Select(d => new PuzzleItem(d.CastTo<PuzzleFile>(), this, log)));
                tracker.Finished++;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load puzzle items", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                Evt.Files.First(f => f.Name == "TOPICS").InitializeTopicFile();
                TopicFile = Evt.Files.First(f => f.Name == "TOPICS");
                tracker.Focus("Topics", TopicFile.TopicStructs.Count);
                foreach (TopicStruct topic in TopicFile.TopicStructs)
                {
                    // Main topics have shadow topics that are located at ID + 40 (this is actually how the game finds them)
                    // So if we're a main topic and we see another topic 40 back, we know we're one of these shadow topics and should really be
                    // rolled into the original main topic
                    if (topic.Type == TopicType.Main && Items.Any(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).Topic.Id == topic.Id - 40))
                    {
                        ((TopicItem)Items.First(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).Topic.Id == topic.Id - 40)).HiddenMainTopic = topic;
                    }
                    else
                    {
                        Items.Add(new TopicItem(topic, this));
                    }
                    tracker.Finished++;
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load topics", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                SystemTextureFile systemTextureFile = Dat.Files.First(f => f.Name == "SYSTEXS").CastTo<SystemTextureFile>();
                tracker.Focus("System Textures",
                    5 + systemTextureFile.SystemTextures.Count(s => Grp.Files.Where(g => g.Name.StartsWith("XTR") || g.Name.StartsWith("SYS") && !g.Name.Contains("_SPC_") && g.Name != "SYS_CMN_B12DNX" && g.Name != "SYS_PPT_001DNX").Select(g => g.Index).Distinct().Contains(s.GrpIndex)));
                Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.Files.First(g => g.Name == "LOGO_CO_SEGDNX").Index), this, "SYSTEX_LOGO_SEGA", height: 192));
                tracker.Finished++;
                Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.Files.First(g => g.Name == "LOGO_CO_AQIDNX").Index), this, "SYSTEX_LOGO_AQI", height: 192));
                tracker.Finished++;
                Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.Files.First(g => g.Name == "LOGO_MW_ACTDNX").Index), this, "SYSTEX_LOGO_MOBICLIP", height: 192));
                tracker.Finished++;
                string criLogoName = Grp.Files.Any(f => f.Name == "CREDITS") ? "SYSTEX_LOGO_HAROOHIE" : "SYSTEX_LOGO_CRIWARE";
                Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.Files.First(g => g.Name == "LOGO_MW_CRIDNX").Index), this, criLogoName, height: 192));
                tracker.Finished++;
                if (Grp.Files.Any(f => f.Name == "CREDITS"))
                {
                    Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.Files.First(g => g.Name == "CREDITS").Index), this, "SYSTEX_LOGO_CREDITS", height: 192));
                }
                tracker.Finished++;
                foreach (SystemTexture extraSysTex in systemTextureFile.SystemTextures.Where(s => Grp.Files.Where(g => g.Name.StartsWith("XTR")).Distinct().Select(g => g.Index).Contains(s.GrpIndex)))
                {
                    Items.Add(new SystemTextureItem(extraSysTex, this, $"SYSTEX_{Grp.Files.First(g => g.Index == extraSysTex.GrpIndex).Name[0..^3]}"));
                    tracker.Finished++;
                }
                // Exclude B12 as that's the nameplates we replace in the character items and PPT_001 as that's the puzzle phase singularity we'll be replacing in the puzzle items
                // We also exclude the "special" graphics as they do not include all of them in the SYSTEX file (should be made to be edited manually)
                foreach (SystemTexture sysSysTex in systemTextureFile.SystemTextures.Where(s => Grp.Files.Where(g => g.Name.StartsWith("SYS") && !g.Name.Contains("_SPC_") && g.Name != "SYS_CMN_B12DNX" && g.Name != "SYS_PPT_001DNX").Select(g => g.Index).Contains(s.GrpIndex)).DistinctBy(s => s.GrpIndex))
                {
                    if (Grp.Files.First(g => g.Index == sysSysTex.GrpIndex).Name[0..^4].EndsWith("T6"))
                    {
                        // special case the ep headers
                        Items.Add(new SystemTextureItem(sysSysTex, this, $"SYSTEX_{Grp.Files.First(g => g.Index == sysSysTex.GrpIndex).Name[0..^3]}", height: 192));
                    }
                    else
                    {
                        Items.Add(new SystemTextureItem(sysSysTex, this, $"SYSTEX_{Grp.Files.First(g => g.Index == sysSysTex.GrpIndex).Name[0..^3]}"));
                    }
                    tracker.Finished++;
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load system textures", ex);
                return new(LoadProjectState.FAILED);
            }

            EventFile scenarioFile;
            try
            {
                // Scenario item must be created after script and puzzle items are constructed
                tracker.Focus("Scenario", 1);
                scenarioFile = Evt.Files.First(f => f.Name == "SCENARIOS");
                scenarioFile.InitializeScenarioFile();
                Items.Add(new ScenarioItem(scenarioFile.Scenario, this, log));
                tracker.Finished++;
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load scenario", ex);
                return new(LoadProjectState.FAILED);
            }

            try
            {
                tracker.Focus("Group Selections", scenarioFile.Scenario.Selects.Count);
                for (int i = 0; i < scenarioFile.Scenario.Selects.Count; i++)
                {
                    Items.Add(new GroupSelectionItem(scenarioFile.Scenario.Selects[i], i, this));
                    tracker.Finished++;
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load group selections", ex);
                return new(LoadProjectState.FAILED);
            }

            if (ItemNames is null)
            {
                try
                {
                    ItemNames = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultNames.json")));
                    foreach (ItemDescription item in Items)
                    {
                        if (!ItemNames.ContainsKey(item.Name) && item.CanRename)
                        {
                            ItemNames.Add(item.Name, item.DisplayName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogException($"Failed to load item names", ex);
                    return new(LoadProjectState.FAILED);
                }
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].CanRename || Items[i].Type == ItemDescription.ItemType.Place) // We don't want to manually rename places, but they do use the display name pattern
                {
                    if (ItemNames.ContainsKey(Items[i].Name))
                    {
                        Items[i].Rename(ItemNames[Items[i].Name]);
                    }
                    else
                    {
                        ItemNames.Add(Items[i].Name, Items[i].DisplayName);
                    }
                }
            }

            return new(LoadProjectState.SUCCESS);
        }

        public bool VoiceMapIsV06OrHigher()
        {
            return Evt.Files.Any(f => f.Name == "VOICEMAPS") && Encoding.ASCII.GetString(Evt.Files.First(f => f.Name == "VOICEMAPS").Data.Skip(0x08).Take(4).ToArray()) == "SUBS";
        }

        public void MigrateProject(string newRom, ILogger log, IProgressTracker tracker)
        {
            log.Log($"Attempting to migrate base ROM to {newRom}");

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            NdsProjectFile.Create("temp", newRom, tempDir);
            IO.CopyFiles(Path.Combine(tempDir, "data"), Path.Combine(BaseDirectory, "original", "archives"), log, "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "data", "bgm"), Path.Combine(BaseDirectory, "original", "bgm"), log, "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "data", "vce"), Path.Combine(BaseDirectory, "original", "vce"), log, "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "overlay"), Path.Combine(BaseDirectory, "original", "overlay"), log, "*.bin");
            IO.CopyFiles(Path.Combine(tempDir, "data", "movie"), Path.Combine(BaseDirectory, "rom", "data", "movie"), log, "*.mods");

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
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            return Items.FirstOrDefault(i => name.Contains(" - ") ? i.Name == name.Split(" - ")[0] : i.DisplayName == name);
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
                Project project = JsonSerializer.Deserialize<Project>(File.ReadAllText(projFile), SERIALIZER_OPTIONS);
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

        public void Save(ILogger log)
        {
            try
            {
                File.WriteAllText(Path.Combine(MainDirectory, $"{Name}.{PROJECT_FORMAT}"), JsonSerializer.Serialize<Project>(this, SERIALIZER_OPTIONS));
            }
            catch (Exception ex)
            {
                log.LogException("Failed to save project file! Check logs for more information.", ex);
            }
        }

        public List<ItemDescription> GetSearchResults(string query, ILogger logger)
        {
            return GetSearchResults(SearchQuery.Create(query), logger);
        }

        public List<ItemDescription> GetSearchResults(SearchQuery query, ILogger log, IProgressTracker? tracker = null)
        {
            var term = query.Term.Trim();
            var searchable = Items.Where(i => query.Types.Contains(i.Type)).ToList();
            tracker?.Focus($"{searchable.Count} Items", searchable.Count);

            try
            {
                return searchable.Where(item =>
                {
                    bool hit = query.Scopes.Aggregate(
                        false,
                        (current, scope) => current || ItemMatches(item, term, scope, log)
                    );
                    if (tracker is not null) tracker.Finished++;
                    return hit;
                })
                    .ToList();
            }
            catch (Exception ex)
            {
                log.LogException("Failed to get search results!", ex);
                return Array.Empty<ItemDescription>().ToList();
            }
        }

        private bool ItemMatches(ItemDescription item, string term, SearchQuery.DataHolder scope, ILogger logger)
        {
            switch (scope)
            {
                case SearchQuery.DataHolder.Title:
                    return item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                           item.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase);

                case SearchQuery.DataHolder.Dialogue_Text:
                    if (item is ScriptItem dialogueScript)
                    {
                        if (LangCode.Equals("ja", StringComparison.OrdinalIgnoreCase))
                        {
                            return dialogueScript.GetScriptCommandTree(this, logger)
                                .Any(s => s.Value.Any(c => c.Parameters
                                    .Where(p => p.Type == ScriptParameter.ParameterType.DIALOGUE)
                                    .Any(p => ((DialogueScriptParameter)p).Line.Text
                                        .Contains(term, StringComparison.OrdinalIgnoreCase))));
                        }
                        else
                        {
                            return dialogueScript.GetScriptCommandTree(this, logger)
                                .Any(s => s.Value.Any(c => c.Parameters
                                    .Where(p => p.Type == ScriptParameter.ParameterType.DIALOGUE)
                                    .Any(p => ((DialogueScriptParameter)p).Line.Text
                                        .GetSubstitutedString(this).Contains(term, StringComparison.OrdinalIgnoreCase))));
                        }
                    }
                    return false;

                case SearchQuery.DataHolder.Script_Flag:
                    if (item is ScriptItem flagScript)
                    {
                        return flagScript.GetScriptCommandTree(this, logger)
                            .Any(s => s.Value.Any(c => c.Parameters
                                .Where(p => p.Type == ScriptParameter.ParameterType.FLAG)
                                .Any(p => ((FlagScriptParameter)p).FlagName
                                    .Contains(term, StringComparison.OrdinalIgnoreCase))));
                    }
                    return false;

                case SearchQuery.DataHolder.Conditional:
                    if (item is ScriptItem conditionalScript)
                    {
                        return conditionalScript.Event.ConditionalsSection?.Objects?
                            .Any(c => !string.IsNullOrEmpty(c) && c.Contains(term, StringComparison.OrdinalIgnoreCase)) ?? false;
                    }
                    return false;

                case SearchQuery.DataHolder.Speaker_Name:
                    if (item is ScriptItem speakerScript)
                    {
                        return speakerScript.GetScriptCommandTree(this, logger)
                            .Any(s => s.Value.Any(c => c.Parameters
                                .Where(p => p.Type == ScriptParameter.ParameterType.DIALOGUE)
                                .Any(p => Characters[(int)((DialogueScriptParameter)p).Line.Speaker].Name
                                    .Contains(term, StringComparison.OrdinalIgnoreCase))));
                    }
                    return false;

                case SearchQuery.DataHolder.Background_Type:
                    if (item is BackgroundItem bg)
                    {
                        return bg.BackgroundType.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;

                case SearchQuery.DataHolder.Episode_Number:
                    if (int.TryParse(term, out int episodeNum))
                    {
                        return ItemIsInEpisode(item, episodeNum, unique: false);
                    }
                    return false;

                case SearchQuery.DataHolder.Episode_Unique:
                    if (int.TryParse(term, out int episodeNumUnique))
                    {
                        return ItemIsInEpisode(item, episodeNumUnique, unique: true);
                    }
                    return false;

                default:
                    logger.LogError($"Unimplemented search scope: {scope}");
                    return false;
            }
        }

        private bool ItemIsInEpisode(ItemDescription item, int episodeNum, bool unique)
        {
            int scenarioEpIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.NEW_GAME && c.Parameter == episodeNum);
            if (scenarioEpIndex >= 0)
            {
                int scenarioNextEpIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.NEW_GAME && c.Parameter == episodeNum + 1);
                if (item is ScriptItem script)
                {
                    return ScriptIsInEpisode(script, scenarioEpIndex, scenarioNextEpIndex);
                }
                else
                {
                    List<ItemDescription> references = item.GetReferencesTo(this);
                    if (unique)
                    {

                        return references.Where(r => r.Type == ItemDescription.ItemType.Script).Any() &&
                            references.Where(r => r.Type == ItemDescription.ItemType.Script)
                            .All(r => ScriptIsInEpisode((ScriptItem)r, scenarioEpIndex, scenarioNextEpIndex));
                    }
                    else
                    {
                        return references.Any(r => r.Type == ItemDescription.ItemType.Script && ScriptIsInEpisode((ScriptItem)r, scenarioEpIndex, scenarioNextEpIndex));
                    }
                }
            }
            return false;
        }

        private bool ScriptIsInEpisode(ScriptItem script, int scenarioEpIndex, int scenarioNextEpIndex)
        {
            int scriptFileScenarioIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.LOAD_SCENE && c.Parameter == script.Event.Index);
            if (scriptFileScenarioIndex < 0)
            {
                List<ItemDescription> references = script.GetReferencesTo(this);
                ItemDescription groupSelection = references.Find(r => r.Type == ItemDescription.ItemType.Group_Selection);
                if (groupSelection is not null)
                {
                    scriptFileScenarioIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.ROUTE_SELECT && c.Parameter == ((GroupSelectionItem)groupSelection).Index);
                }
            }
            if (scenarioNextEpIndex < 0)
            {
                scenarioNextEpIndex = int.MaxValue;
            }

            return scriptFileScenarioIndex > scenarioEpIndex && scriptFileScenarioIndex < scenarioNextEpIndex;
        }
    }
}
