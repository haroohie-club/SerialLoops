﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HaroohieClub.NitroPacker;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using HaruhiChokuretsuLib.Util.Exceptions;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.SaveFile;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SkiaSharp;
using static SerialLoops.Lib.Items.ItemDescription;

namespace SerialLoops.Lib;

public partial class Project
{
    public const string PROJECT_FORMAT = "slproj";
    public const string EXPORT_FORMAT = "slzip";
    public static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { Converters = { new SKColorJsonConverter() } };

    public string Name { get; set; }
    public string LangCode { get; set; }
    public string BaseRomHash { get; set; }
    public Dictionary<string, string> ItemNames { get; set; }
    public Dictionary<int, NameplateProperties> Characters { get; set; }

    // SL settings
    [JsonIgnore]
    public string MainDirectory => Path.Combine(Config.ProjectsDirectory, Name);
    [JsonIgnore]
    public string BaseDirectory => Path.Combine(MainDirectory, "base");
    [JsonIgnore]
    public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");
    [JsonIgnore]
    public string ProjectFile => Path.Combine(MainDirectory, $"{Name}.{PROJECT_FORMAT}");
    [JsonIgnore]
    public Config Config { get; set; }
    [JsonIgnore]
    public ProjectSettings Settings { get; set; }
    [JsonIgnore]
    public List<ItemDescription> Items { get; set; } = [];

    // Archives
    [JsonIgnore]
    public ArchiveFile<DataFile> Dat { get; set; }
    [JsonIgnore]
    public ArchiveFile<GraphicsFile> Grp { get; set; }
    [JsonIgnore]
    public ArchiveFile<EventFile> Evt { get; set; }
    [JsonIgnore]
    public SoundArchive Snd { get; set; }

    // Common graphics
    [JsonIgnore]
    public FontReplacementDictionary FontReplacement { get; set; } = [];
    [JsonIgnore]
    public FontFile FontMap { get; set; } = new();
    [JsonIgnore]
    public SKBitmap SpeakerBitmap { get; set; }
    [JsonIgnore]
    public SKBitmap NameplateBitmap { get; set; }
    [JsonIgnore]
    public GraphicInfo NameplateInfo { get; set; }
    [JsonIgnore]
    public SKBitmap DialogueBitmap { get; set; }
    [JsonIgnore]
    public SKBitmap DialogueArrow { get; set; }
    [JsonIgnore]
    public SKBitmap FontBitmap { get; set; }

    // Files shared between items
    [JsonIgnore]
    public CharacterDataFile ChrData { get; set; }
    [JsonIgnore]
    public EventFile EventTableFile { get; set; }
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
    [JsonIgnore]
    public Dictionary<int, GraphicsFile> LayoutFiles { get; set; } = [];
    [JsonIgnore]
    public Dictionary<ChessFile.ChessPiece, SKBitmap> ChessPieceImages { get; private set; }
    [JsonIgnore]
    public GraphicsFile DialoguePaletteFile { get; set; }
    [JsonIgnore]
    public SKColor[] DialogueColors { get; set; } = new SKColor[16];
    [JsonIgnore]
    public SKPaint[] DialogueColorFilters { get; set; } = new SKPaint[16];
    [JsonIgnore]
    public SaveItem ProjectSaveFile { get; set; }

    public float AverageBgmMaxAmplitude { get; set; }

    // Localization function to make localizing accessible from the lib
    [JsonIgnore]
    public Func<string, string> Localize { get; set; }

    private static readonly string[] NON_SCRIPT_EVT_FILES = ["CHESSS", "EVTTBLS", "TOPICS", "SCENARIOS", "TUTORIALS", "VOICEMAPS"];
    private static readonly ItemType[] IGNORED_ORPHAN_TYPES = [ ItemType.Layout, ItemType.Scenario ];

    public Project()
    {
    }

    public Project(string name, string langCode, Config config, Func<string, string> localize, ILogger log)
    {
        Name = name;
        LangCode = langCode;
        Config = config;
        Localize = localize;
        log.Log("Creating project directories...");
        try
        {
            Directory.CreateDirectory(MainDirectory);
            Save(log);
            Directory.CreateDirectory(BaseDirectory);
            Directory.CreateDirectory(IterativeDirectory);
            Directory.CreateDirectory(Path.Combine(MainDirectory, "font"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "charset.json"), Path.Combine(MainDirectory, "font", "charset.json"), overwrite: true);
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

    public static Project Deserialize(string path)
    {
        return JsonSerializer.Deserialize<Project>(File.ReadAllText(path), SERIALIZER_OPTIONS);
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
        if (Directory.GetFiles(Path.Combine(IterativeDirectory, "assets"), "*", SearchOption.AllDirectories).Length > 0)
        {
            return new(LoadProjectState.LOOSELEAF_FILES);
        }
        return LoadArchives(log, tracker);
    }

    public void LoadProjectSettings(ILogger log, IProgressTracker tracker)
    {
        tracker.Focus("Project Settings", 1);
        string projPath = Path.Combine(IterativeDirectory, "rom", $"{Name}.json");
        try
        {
            Settings = new(NdsProjectFile.Deserialize(projPath), log);
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

        try
        {
            // Note that the nameplates are not localized by program locale but by selected project language
            string defaultCharactersJson = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultCharacters")}.{LangCode}.json";
            if (!File.Exists(defaultCharactersJson))
            {
                defaultCharactersJson = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultCharacters")}.en.json";
            }
            Characters ??= JsonSerializer.Deserialize<Dictionary<int, NameplateProperties>>(File.ReadAllText(defaultCharactersJson), SERIALIZER_OPTIONS);
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
            FontMap = Dat.GetFileByName("FONTS").CastTo<FontFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load font map", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            SpeakerBitmap = Grp.GetFileByName("SYS_CMN_B12DNX").GetImage(transparentIndex: 0);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load speaker bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        try
        {
            GraphicsFile nameplate = Grp.GetFileByName("SYS_CMN_B12DNX");
            NameplateBitmap = nameplate.GetImage();
            NameplateInfo = new(nameplate);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load nameplate bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            DialogueBitmap = Grp.GetFileByName("SYS_CMN_B02DNX").GetImage(transparentIndex: 0);
            DialogueArrow = Grp.GetFileByName("SYS_CMN_B17DNX").GetImage(transparentIndex: 0);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load dialogue bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            GraphicsFile fontFile = Grp.GetFileByName("ZENFONTBNF");
            fontFile.InitializeFontFile();
            FontBitmap = Grp.GetFileByName("ZENFONTBNF").GetImage(transparentIndex: 0);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load font bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        tracker.Focus("Dialogue Colors", DialogueColorFilters.Length);
        try
        {
            DialoguePaletteFile = Grp.GetFileByName("FONT0DNX");
            for (int i = 0; i < DialogueColorFilters.Length; i++)
            {
                SKColor color = DialoguePaletteFile.Palette[i * 16 + 15];
                DialogueColors[i] = color;
                DialogueColorFilters[i] = color.GetColorFilter();
                tracker.Finished++;
            }
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load dialogue colors", ex);
            return new(LoadProjectState.FAILED);
        }

        tracker.Focus("Chess Piece Images", 12);
        try
        {
            ChessPieceImages = [];
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteKing,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteKing, Grp));
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackKing,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackKing, Grp));
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteQueen,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteQueen, Grp));
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackQueen,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackQueen, Grp));
            tracker.Finished++;
            SKBitmap whiteBishop = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteBishopLeft, Grp);
            tracker.Finished++;
            SKBitmap blackBishop = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackBishopLeft, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteBishopLeft, whiteBishop);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteBishopRight, whiteBishop);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackBishopLeft, blackBishop);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackBishopRight, blackBishop);
            SKBitmap whiteKnight = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteKnightLeft, Grp);
            tracker.Finished++;
            SKBitmap blackKnight = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackKnightLeft, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteKnightLeft, whiteKnight);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteKnightRight, whiteKnight);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackKnightLeft, blackKnight);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackKnightRight, blackKnight);
            SKBitmap whiteRook = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteRookLeft, Grp);
            tracker.Finished++;
            SKBitmap blackRook = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackRookLeft, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteRookLeft, whiteRook);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteRookRight, whiteRook);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackRookLeft, blackRook);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackRookRight, blackRook);
            SKBitmap whitePawn = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhitePawnA, Grp);
            tracker.Finished++;
            SKBitmap blackPawn = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackPawnA, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnA, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnB, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnC, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnD, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnE, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnF, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnG, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnH, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnA, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnB, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnC, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnD, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnE, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnF, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnG, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnH, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.Empty, new(16, 32));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load chess pieces", ex);
            return new(LoadProjectState.FAILED);
        }

        tracker.Focus("Static Files", 6);
        try
        {
            EventTableFile = Evt.GetFileByName("EVTTBLS");
            EventTableFile.InitializeEventTableFile();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load event table file", ex);
        }
        tracker.Finished++;
        try
        {
            ChrData = Dat.GetFileByName("CHRDATAS").CastTo<CharacterDataFile>();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load chrdata file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            Extra = Dat.GetFileByName("EXTRAS").CastTo<ExtraFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load extra file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            EventFile scenario = Evt.GetFileByName("SCENARIOS");
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
            MessInfo = Dat.GetFileByName("MESSINFOS").CastTo<MessageInfoFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load message info file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            UiText = Dat.GetFileByName("MESSS").CastTo<MessageFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load UI text file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        try
        {
            BgTableFile bgTable = Dat.GetFileByName("BGTBLS").CastTo<BgTableFile>();
            tracker.Focus("Backgrounds", bgTable.BgTableEntries.Count);
            List<string> names = [];
            Items.AddRange(bgTable.BgTableEntries.AsParallel().Select((entry, i) =>
            {
                if (entry.BgIndex1 > 0)
                {
                    GraphicsFile nameGraphic = Grp.GetFileByIndex(entry.BgIndex1);
                    string name = $"BG_{nameGraphic.Name[..nameGraphic.Name.LastIndexOf('_')]}";
                    string bgNameBackup = name;
                    for (int j = 1; names.Contains(name); j++)
                    {
                        name = $"{bgNameBackup}{j:D2}";
                    }
                    tracker.Finished++;
                    names.Add(name);
                    return new BackgroundItem(name, i, entry, this);
                }
                else
                {
                    return null;
                }
            }).Where(b => b is not null));
        }
        catch (Exception ex)
        {
            log.LogException("Failed to background items", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            SoundDS = Dat.GetFileByName("SND_DSS").CastTo<SoundDSFile>();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load DS sound file.", ex);
        }
        try
        {
            if (VoiceMapIsV06OrHigher() || VoiceMapIsV08OrHigher())
            {
                VoiceMap = Evt.GetFileByName("VOICEMAPS").CastTo<VoiceMapFile>();
            }
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load voice map", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            string[] bgmFiles = SoundDS.BgmSection.AsParallel().Where(bgm => bgm is not null).Select(bgm => Path.Combine(IterativeDirectory, "rom", "data", bgm)).ToArray();
            tracker.Focus("BGM Tracks", bgmFiles.Length);
            List<float> maxes = [];
            Items.AddRange(bgmFiles.AsParallel().Select((bgm, i) =>
            {
                tracker.Finished++;
                BackgroundMusicItem bgmItem = new(bgm, i, this);
                if (AverageBgmMaxAmplitude == 0)
                {
                    maxes.Add(bgmItem.GetWaveProvider(log, false).GetMaxAmplitude(log));
                }
                return bgmItem;
            }));

            if (AverageBgmMaxAmplitude == 0)
            {
                AverageBgmMaxAmplitude = maxes.Average();
                Save(log);
            }
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load BGM tracks", ex);
            return new(LoadProjectState.FAILED);
        }
        try
        {
            string[] voiceFiles = SoundDS.VoiceSection.AsParallel().Where(vce => vce is not null).Select(vce => Path.Combine(IterativeDirectory, "rom", "data", vce)).ToArray();
            tracker.Focus("Voiced Lines", voiceFiles.Length);
            Items.AddRange(voiceFiles.AsParallel().Select((vce, i) =>
            {
                tracker.Finished++;
                return new VoicedLineItem(vce, i + 1, this);
            }));
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
            tracker.Focus("Chess Puzzles", Dat.Files.Count(f => f.Name.StartsWith("CHS")));
            Items.AddRange(Dat.Files.AsParallel()
                .Where(f => f.Name.StartsWith("CHS"))
                .Select(f =>
                {
                    tracker.Finished++;
                    return new ChessPuzzleItem(f.CastTo<ChessFile>());
                }));
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load chess puzzles", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            ItemFile itemFile = Dat.GetFileByName("ITEMS").CastTo<ItemFile>();
            tracker.Focus("Items", itemFile.Items.Count);
            Items.AddRange(itemFile.Items.AsParallel().Where(i => i > 0).Select((i, idx) =>
            {
                tracker.Finished++;
                return new ItemItem(Grp.GetFileByIndex(i).Name, idx, i, this);
            }));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load item file", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Characters", MessInfo.MessageInfos.Count);
            Items.AddRange(MessInfo.MessageInfos.AsParallel().Where(m => (int)m.Character > 0).Select(m =>
            {
                tracker.Finished++;
                return new CharacterItem(m, Characters[(int)m.Character], this);
            }));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load characters", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Character Sprites", ChrData.Sprites.Count);
            Items.AddRange(ChrData.Sprites.AsParallel().Where(s => (int)s.Character > 0).Select(s =>
            {
                tracker.Finished++;
                return new CharacterSpriteItem(s, ChrData, this, log);
            }));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load character sprites", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            ChibiFile chibiFile = Dat.GetFileByName("CHIBIS").CastTo<ChibiFile>();
            tracker.Focus("Chibis", chibiFile.Chibis.Count);
            Items.AddRange(chibiFile.Chibis.AsParallel().Select((c, i) =>
            {
                tracker.Finished++;
                return new ChibiItem(c, i, this);
            }));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load chibis", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Scripts", Evt.Files.Count - NON_SCRIPT_EVT_FILES.Length);
            Items.AddRange(Evt.Files.AsParallel()
                .Where(e => !NON_SCRIPT_EVT_FILES.Contains(e?.Name))
                .Select(e =>
                {
                    tracker.Finished++;
                    return new ScriptItem(e, EventTableFile.EvtTbl, Localize, log);
                }));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load scripts", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            TutorialFile = Evt.GetFileByName("TUTORIALS");
            TutorialFile.InitializeTutorialFile();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load tutorials", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            QMapFile qmap = Dat.GetFileByName("QMAPS").CastTo<QMapFile>();
            tracker.Focus("Maps", qmap.QMaps.Count);
            Items.AddRange(Dat.Files.AsParallel()
                .Where(d => qmap.QMaps.Select(q => q.Name.Replace(".", "")).Contains(d.Name))
                .Select(m =>
                {
                    tracker.Finished++;
                    return new MapItem(m.CastTo<MapFile>(), qmap.QMaps.FindIndex(q => q.Name.Replace(".", "") == m.Name), this);
                }));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load maps", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            PlaceFile placeFile = Dat.GetFileByName("PLACES").CastTo<PlaceFile>();
            tracker.Focus("Places", placeFile.PlaceGraphicIndices.Count);
            Items.AddRange(placeFile.PlaceGraphicIndices.Select((pidx, i) =>
            {
                tracker.Finished++;
                return new PlaceItem(i, Grp.GetFileByIndex(pidx));
            }));
            tracker.Finished++;
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load place items", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            var puzzleDats = Dat.Files.AsParallel().Where(d => d.Name.StartsWith("SLG"));
            tracker.Focus("Puzzles", puzzleDats.Count());
            Items.AddRange(puzzleDats.Select(d =>
            {
                tracker.Finished++;
                return new PuzzleItem(d.CastTo<PuzzleFile>(), this, log);
            }));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load puzzle items", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            TopicFile = Evt.GetFileByName("TOPICS");
            TopicFile.InitializeTopicFile();
            tracker.Focus("Topics", TopicFile.Topics.Count);
            foreach (Topic topic in TopicFile.Topics)
            {
                // Main topics have shadow topics that are located at ID + 40 (this is actually how the game finds them)
                // So if we're a main topic and we see another topic 40 back, we know we're one of these shadow topics and should really be
                // rolled into the original main topic
                if (topic.Type == TopicType.Main && Items.AsParallel().Any(i => i.Type == ItemType.Topic && ((TopicItem)i).TopicEntry.Id == topic.Id - 40))
                {
                    ((TopicItem)Items.AsParallel().First(i => i.Type == ItemType.Topic && ((TopicItem)i).TopicEntry.Id == topic.Id - 40)).HiddenMainTopic = topic;
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
            SystemTextureFile systemTextureFile = Dat.GetFileByName("SYSTEXS").CastTo<SystemTextureFile>();
            tracker.Focus("System Textures",
                5 + systemTextureFile.SystemTextures.Count(s => Grp.Files.AsParallel().Where(g => g.Name.StartsWith("XTR") || g.Name.StartsWith("SYS") && !g.Name.Contains("_SPC_") && g.Name != "SYS_CMN_B12DNX" && g.Name != "SYS_PPT_001DNX").Select(g => g.Index).Distinct().Contains(s.GrpIndex)));
            Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_CO_SEGDNX").Index), this, "SYSTEX_LOGO_SEGA", height: 192));
            tracker.Finished++;
            Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_CO_AQIDNX").Index), this, "SYSTEX_LOGO_AQI", height: 192));
            tracker.Finished++;
            Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_MW_ACTDNX").Index), this, "SYSTEX_LOGO_MOBICLIP", height: 192));
            tracker.Finished++;
            string criLogoName = Grp.Files.AsParallel().Any(f => f.Name == "CREDITS") ? "SYSTEX_LOGO_HAROOHIE" : "SYSTEX_LOGO_CRIWARE";
            Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_MW_CRIDNX").Index), this, criLogoName, height: 192));
            tracker.Finished++;
            if (Grp.Files.Any(f => f.Name == "CREDITS"))
            {
                Items.Add(new SystemTextureItem(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("CREDITS").Index), this, "SYSTEX_LOGO_CREDITS", height: 192));
            }
            tracker.Finished++;
            foreach (SystemTexture extraSysTex in systemTextureFile.SystemTextures.Where(s => Grp.Files.AsParallel().Where(g => g.Name.StartsWith("XTR")).Distinct().Select(g => g.Index).Contains(s.GrpIndex)))
            {
                Items.Add(new SystemTextureItem(extraSysTex, this, $"SYSTEX_{Grp.GetFileByIndex(extraSysTex.GrpIndex).Name[..^3]}"));
                tracker.Finished++;
            }
            // Exclude B12 as that's the nameplates we replace in the character items and PPT_001 as that's the puzzle phase singularity we'll be replacing in the puzzle items
            // We also exclude the "special" graphics as they do not include all of them in the SYSTEX file (should be made to be edited manually)
            foreach (SystemTexture sysSysTex in systemTextureFile.SystemTextures.Where(s => Grp.Files.AsParallel().Where(g => g.Name.StartsWith("SYS") && !g.Name.Contains("_SPC_") && g.Name != "SYS_CMN_B12DNX" && g.Name != "SYS_PPT_001DNX").Select(g => g.Index).Contains(s.GrpIndex)).DistinctBy(s => s.GrpIndex))
            {
                if (Grp.GetFileByIndex(sysSysTex.GrpIndex).Name[..^4].EndsWith("T6"))
                {
                    // special case the ep headers
                    Items.Add(new SystemTextureItem(sysSysTex, this, $"SYSTEX_{Grp.GetFileByIndex(sysSysTex.GrpIndex).Name[..^3]}", height: 192));
                }
                else
                {
                    Items.Add(new SystemTextureItem(sysSysTex, this, $"SYSTEX_{Grp.GetFileByIndex(sysSysTex.GrpIndex).Name[..^3]}"));
                }
                tracker.Finished++;
            }
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load system textures", ex);
            return new(LoadProjectState.FAILED);
        }

        // We're gonna try to do more research on this later but for now we're going to hardcode these values
        try
        {
            LayoutFiles.Clear();
            tracker.Focus("Layouts", 22);

            // Puzzle phase layouts
            LayoutFiles.Add(0xC45, Grp.GetFileByIndex(0xC45));
            List <GraphicsFile> puzzlePhaseGraphics =
            [
                Grp.GetFileByIndex(0xC48),
                Grp.GetFileByIndex(0xC4A),
                Grp.GetFileByIndex(0xC4C),
                Grp.GetFileByIndex(0xC4D),
                Grp.GetFileByIndex(0xC4F),
                Grp.GetFileByIndex(0xC50),
                Grp.GetFileByIndex(0xC52),
                Grp.GetFileByIndex(0xC54),
                Grp.GetFileByIndex(0xC55),
                Grp.GetFileByIndex(0xC57),
                Grp.GetFileByIndex(0xC59),
                Grp.GetFileByIndex(0xC5B),
                Grp.GetFileByIndex(0xC5D),
                Grp.GetFileByIndex(0xC60),
                Grp.GetFileByIndex(0xC61),
                Grp.GetFileByIndex(0xC62),
                Grp.GetFileByIndex(0xC63),
                Grp.GetFileByIndex(0xC64),
                Grp.GetFileByIndex(0xC65),
                Grp.GetFileByIndex(0xC66),
                Grp.GetFileByIndex(0xC67),
            ];

            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 54, 13, "LYT_ACCIDENT_OUTBREAK", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 67, 5, "LYT_MAIN_TOPIC_DELAYED", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 72, 12, "LYT_DELAY_CHANCE", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 84, 2, "LYT_TOPIC_CHOOSE", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 122, 8, "LYT_READY", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 130, 3, "LYT_GO", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 134, 4, "LYT_TIME_RESULT", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 138, 2, "LYT_ACCIDENT_RESULT", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 140, 2, "LYT_POWER_UP_RESULT", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 142, 2, "LYT_BASE_TIME_LIMIT", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 148, 2, "LYT_HRH_DISTRACTION_BONUS", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 154, 5, "LYT_TOTAL_SCORE", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 163, 3, "LYT_MAIN_TOPICS_OBTAINED", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 163, 3, "LYT_ACCIDENT_BUTTON", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 175, 2, "LYT_MAIN_TOPIC", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 177, 1, "LYT_COUNTER", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 199, 27, "LYT_CHARACTER_TOPICS_OBTAINED", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 226, 4, "LYT_TIME_LIMIT", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 235, 2, "LYT_ACCIDENT_AVOIDED", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 286, 2, "LYT_SEARCH_BUTTON", this));
            tracker.Finished++;
            Items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 307, 1, "LYT_MIN_ERASED_GOAL", this));
            tracker.Finished++;
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load layouts", ex);
            return new(LoadProjectState.FAILED);
        }

        EventFile scenarioFile;
        try
        {
            // Scenario item must be created after script and puzzle items are constructed
            tracker.Focus("Scenario", 1);
            scenarioFile = Evt.GetFileByName("SCENARIOS");
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
            Items.AddRange(scenarioFile.Scenario.Selects.Select((s, i) =>
            {
                tracker.Finished++;
                return new GroupSelectionItem(s, i, this);
            }));
            ((ScenarioItem)Items.First(i => i.Type == ItemType.Scenario)).Refresh(this, log);
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
                ItemNames = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Extensions.GetLocalizedFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultNames"), "json")));
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
            if (Items[i].CanRename || Items[i].Type == ItemType.Place) // We don't want to manually rename places, but they do use the display name pattern
            {
                if (ItemNames.TryGetValue(Items[i].Name, out string value))
                {
                    Items[i].DisplayName = value;
                }
                else
                {
                    ItemNames.Add(Items[i].Name, Items[i].DisplayName);
                }
            }
        }

        Items = [.. Items.OrderBy(i => i.DisplayName)];

        return new(LoadProjectState.SUCCESS);
    }

    public bool VoiceMapIsV06OrHigher()
    {
        return Evt.Files.AsParallel().Any(f => f.Name == "VOICEMAPS") && Encoding.ASCII.GetString(Evt.GetFileByName("VOICEMAPS").Data.Skip(0x08).Take(4).ToArray()) == "SUBS";
    }

    public bool VoiceMapIsV08OrHigher()
    {
        return Evt.Files.AsParallel().Any(f => f.Name == "VOICEMAPS") && Encoding.ASCII.GetString(Evt.GetFileByName("VOICEMAPS").Data.Skip(0x08).Take(4).ToArray()) == "SUB2";
    }

    public void RecalculateEventTable()
    {
        short currentFlag = 0;
        int prevScriptIndex = 0;
        foreach (EventTableEntry entry in EventTableFile.EvtTbl.Entries)
        {
            if (currentFlag == 0 && entry.FirstReadFlag > 0)
            {
                currentFlag = entry.FirstReadFlag;
                prevScriptIndex = entry.EventFileIndex;
            }
            else if (entry.FirstReadFlag > 0)
            {
                currentFlag += (short)(Evt.GetFileByIndex(prevScriptIndex).ScriptSections.Count + 1);
                entry.FirstReadFlag = currentFlag;
                prevScriptIndex = entry.EventFileIndex;
            }
        }
        Items.Where(i => i.Type == ItemType.Script).Cast<ScriptItem>().ToList().ForEach(s => s.UpdateEventTableInfo(EventTableFile.EvtTbl));
    }

    public void MigrateProject(string newRom, Config config, ILogger log, IProgressTracker tracker)
    {
        log.Log($"Attempting to migrate base ROM to {newRom}");

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        NdsProjectFile.Create("temp", newRom, tempDir);
        IO.CopyFiles(Path.Combine(tempDir, "data"), Path.Combine(BaseDirectory, "original", "archives"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "data", "bgm"), Path.Combine(BaseDirectory, "original", "bgm"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "data", "vce"), Path.Combine(BaseDirectory, "original", "vce"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "overlay"), Path.Combine(BaseDirectory, "original", "overlay"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "data", "movie"), Path.Combine(BaseDirectory, "rom", "data", "movie"), log, "*.mods");
        File.Copy(Path.Combine(tempDir, "arm9.bin"), Path.Combine(BaseDirectory, "rom", "arm9.bin"), overwrite: true);
        File.Copy(Path.Combine(tempDir, "arm9.bin"), Path.Combine(BaseDirectory, "src", "arm9.bin"), overwrite: true);
        File.Copy(Path.Combine(tempDir, "arm9.bin"), Path.Combine(IterativeDirectory, "rom", "arm9.bin"), overwrite: true);
        File.Copy(Path.Combine(tempDir, "arm9.bin"), Path.Combine(IterativeDirectory, "src", "arm9.bin"), overwrite: true);

        Build.BuildBase(this, config, log, tracker);

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

        return Items.FirstOrDefault(i => i.DisplayName == name);
    }

    public static (Project Project, LoadProjectResult Result) OpenProject(string projFile, Config config, Func<string, string> localize, ILogger log, IProgressTracker tracker)
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
            Project project = Deserialize(projFile);
            project.Localize = localize;
            tracker.Finished++;

            // If we detect an old NP format, auto-migrate it
            if (!File.Exists(Path.Combine(config.ProjectsDirectory, project.Name, "base", "rom", $"{project.Name}.json")))
            {
                NdsProjectFile.ConvertProjectFile(Path.Combine(config.ProjectsDirectory, project.Name, "base", "rom", $"{project.Name}.xml"));
                NdsProjectFile.ConvertProjectFile(Path.Combine(config.ProjectsDirectory, project.Name, "iterative", "rom", $"{project.Name}.xml"));
                NdsProjectFile.ConvertProjectFile(Path.Combine(config.ProjectsDirectory, project.Name, "base", "original", $"{project.Name}.xml"));
            }

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
        catch (Exception ex)
        {
            log.LogException(localize("Error while loading project"), ex);
            return (null, new(LoadProjectState.FAILED));
        }
    }

    public void Save(ILogger log)
    {
        try
        {
            File.WriteAllText(ProjectFile, JsonSerializer.Serialize(this, SERIALIZER_OPTIONS));
        }
        catch (Exception ex)
        {
            log.LogException("Failed to save project file! Check logs for more information.", ex);
        }
    }

    public void Export(string slzipFile, ILogger log)
    {
        try
        {
            log.Log($"Creating slzip at '{slzipFile}'...");
            using FileStream slzipFs = File.Create(slzipFile);
            using ZipArchive slzip = new(slzipFs, ZipArchiveMode.Create);
            log.Log($"Adding '{ProjectFile}' to slzip...");
            slzip.CreateEntryFromFile(ProjectFile, Path.GetFileName(ProjectFile)!);
            slzip.Comment = BaseRomHash;
            log.Log("Adding charset.json to slzip...");
            slzip.CreateEntryFromFile(Path.Combine(MainDirectory, "font", "charset.json"), Path.Combine("font", "charset.json").ToUnixPath());
            foreach (string file in Directory.GetFiles(BaseDirectory, "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file).ToUnixPath());
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "assets"), "*", SearchOption.AllDirectories))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file).ToUnixPath());
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "src"), "*", SearchOption.AllDirectories))
            {
                if (file.Equals("arm9.bin", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file).ToUnixPath());
            }
            slzip.CreateEntryFromFile(Path.Combine(BaseDirectory, "rom", $"{Name}.json"), Path.Combine("base", "rom", $"{Name}.json").ToUnixPath());
            slzip.CreateEntryFromFile(Path.Combine(BaseDirectory, "rom", "banner.bin"), Path.Combine("base", "rom", "banner.bin").ToUnixPath());
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "rom", "data", "bgm"), "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file).ToUnixPath());
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "rom", "data", "movie"), "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file).ToUnixPath());
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "rom", "data", "vce"), "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file).ToUnixPath());
            }
        }
        catch (Exception ex)
        {
            log.LogException(Localize("Failed to export project"), ex);
        }
    }

    public static (Project Project, LoadProjectResult LoadResult) Import(string slzipFile, string romPath, Config config, Func<string, string> localize, ILogger log, IProgressTracker tracker)
    {
        try
        {
            using FileStream slzipFs = File.OpenRead(slzipFile);
            using ZipArchive slzip = new(slzipFs, ZipArchiveMode.Read);
            string slprojTemp = Path.GetTempFileName();
            slzip.Entries.FirstOrDefault(f => f.Name.EndsWith(".slproj"))?.ExtractToFile(slprojTemp, overwrite: true);
            Project project = Deserialize(slprojTemp);
            project.Config = config;
            File.Delete(slprojTemp);
            string oldProjectName = project.Name;
            while (Directory.Exists(project.MainDirectory))
            {
                Match numEnding = ProjectNameAppendedNumber().Match(project.Name);
                if (numEnding.Success)
                {
                    project.Name = project.Name.Replace(numEnding.Value, $"({int.Parse(numEnding.Groups["num"].Value) + 1})");
                }
                else
                {
                    project.Name = $"{project.Name} (1)";
                }
            }

            IO.OpenRom(project, romPath, log, tracker);
            slzip.ExtractToDirectory(project.MainDirectory, overwriteFiles: true);
            string newNdsProjFile = Path.Combine("rom", $"{project.Name}.json");
            if (!project.Name.Equals(oldProjectName))
            {
                string oldNdsProjFile = Path.Combine("rom", $"{oldProjectName}.json");
                File.Move(Path.Combine(project.BaseDirectory, oldNdsProjFile), Path.Combine(project.BaseDirectory, newNdsProjFile), overwrite: true);
            }
            project.Settings = new(NdsProjectFile.Deserialize(Path.Combine(project.BaseDirectory, newNdsProjFile)), log);
            Directory.CreateDirectory(project.IterativeDirectory);
            IO.CopyFiles(project.BaseDirectory, project.IterativeDirectory, log, recursive: true);
            Build.BuildBase(project, config, log, tracker);

            return (project, project.Load(config, log, tracker));
        }
        catch (Exception ex)
        {
            log.LogException(localize("Failed to import project"), ex);
            return (null, new() { State = LoadProjectState.FAILED });
        }
    }

    public void SetBaseRomHash(string romPath)
    {
        BaseRomHash = string.Join("", SHA1.HashData(File.ReadAllBytes(romPath)).Select(b => $"{b:X2}"));
    }

    public List<ItemDescription> GetSearchResults(string query, ILogger logger)
    {
        return GetSearchResults(SearchQuery.Create(query), logger);
    }

    public List<ItemDescription> GetSearchResults(SearchQuery query, ILogger log, IProgressTracker tracker = null)
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
            return [];
        }
    }

    public CharacterItem GetCharacterBySpeaker(Speaker speaker)
    {
        return (CharacterItem)Items.First(i => i.Type == ItemType.Character && i.DisplayName == $"CHR_{Characters[(int)speaker].Name}");
    }

    public bool LoadProjectSave()
    {
        string savPath = Path.Combine(MainDirectory, $"{Name}.sav");
        if (!File.Exists(savPath))
        {
            return false;
        }

        ProjectSaveFile = new(savPath, $"{Name}.sav");
        return ProjectSaveFile is not null;
    }

    private bool ItemMatches(ItemDescription item, string term, SearchQuery.DataHolder scope, ILogger logger)
    {
        switch (scope)
        {
            case SearchQuery.DataHolder.Title:
                return item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                       item.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase);

            case SearchQuery.DataHolder.Background_ID:
                if (int.TryParse(term, out int backgroundId))
                {
                    return item.Type == ItemType.Background && ((BackgroundItem)item).Id == backgroundId;
                }
                return false;

            case SearchQuery.DataHolder.Archive_Index:
                if (int.TryParse(term, out int archiveIdx) || ((term?.StartsWith("0x") ?? false) && int.TryParse(term[2..], NumberStyles.HexNumber, CultureInfo.CurrentCulture.NumberFormat, out archiveIdx)))
                {
                    switch (item.Type)
                    {
                        case ItemType.Background:
                            return ((BackgroundItem)item).Graphic1.Index == archiveIdx || (((BackgroundItem)item).Graphic2?.Index ?? -1) == archiveIdx;
                        case ItemType.Character:
                            return MessInfo.Index == archiveIdx;
                        case ItemType.Character_Sprite:
                            CharacterSpriteItem sprite = (CharacterSpriteItem)item;
                            return new[]
                                       {
                                           sprite.Sprite.TextureIndex1, sprite.Sprite.TextureIndex2,
                                           sprite.Sprite.TextureIndex3, sprite.Sprite.LayoutIndex,
                                           sprite.Sprite.EyeAnimationIndex, sprite.Sprite.MouthAnimationIndex,
                                           sprite.Sprite.EyeTextureIndex, sprite.Sprite.MouthTextureIndex
                                       }
                                       .Contains((short)archiveIdx);
                        case ItemType.Chess_Puzzle:
                            return ((ChessPuzzleItem)item).ChessPuzzle.Index == archiveIdx;
                        case ItemType.Chibi:
                            return ((ChibiItem)item).Chibi.ChibiEntries.Select(c => c.Texture)
                                .Contains((short)archiveIdx) || ((ChibiItem)item).Chibi.ChibiEntries
                                .Select(c => c.Animation).Contains((short)archiveIdx);
                        case ItemType.Group_Selection:
                        case ItemType.Scenario:
                            return archiveIdx == Evt.GetFileByName("SCENARIOS").Index;
                        case ItemType.Item:
                            return ((ItemItem)item).ItemGraphic.Index == archiveIdx;
                        case ItemType.Layout:
                            return ((LayoutItem)item).Layout.Index == archiveIdx;
                        case ItemType.Map:
                            return ((MapItem)item).Map.Index == archiveIdx;
                        case ItemType.Place:
                            return ((PlaceItem)item).PlaceGraphic.Index == archiveIdx;
                        case ItemType.Puzzle:
                            return ((PuzzleItem)item).Puzzle.Index == archiveIdx;
                        case ItemType.Script:
                            return ((ScriptItem)item).Event.Index == archiveIdx;
                        case ItemType.System_Texture:
                            return ((SystemTextureItem)item).SysTex.GrpIndex == archiveIdx;
                        case ItemType.Topic:
                            return archiveIdx == Evt.GetFileByName("TOPICS").Index;
                    }
                }
                return false;

            case SearchQuery.DataHolder.Archive_Filename:
                switch (item.Type)
                {
                    case ItemType.Background:
                        return ((BackgroundItem)item).Graphic1.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || (((BackgroundItem)item).Graphic2?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
                    case ItemType.Character:
                        return MessInfo.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Character_Sprite:
                        CharacterSpriteItem sprite = (CharacterSpriteItem)item;
                        string[] sprNames = [.. new[]
                        {
                            sprite.Sprite.TextureIndex1, sprite.Sprite.TextureIndex2, sprite.Sprite.TextureIndex3,
                            sprite.Sprite.LayoutIndex, sprite.Sprite.EyeAnimationIndex,
                            sprite.Sprite.MouthAnimationIndex, sprite.Sprite.EyeTextureIndex,
                            sprite.Sprite.MouthTextureIndex
                        }.Select(i => Grp.GetFileByIndex(i).Name)];
                        return sprNames.Any(n => n.Contains(term, StringComparison.OrdinalIgnoreCase));
                    case ItemType.Chess_Puzzle:
                        return ((ChessPuzzleItem)item).ChessPuzzle.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Chibi:
                        return ((ChibiItem)item).Chibi.ChibiEntries.Select(c => Grp.GetFileByIndex(c.Texture)?.Name)
                            .Any(n => n?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                            || ((ChibiItem)item).Chibi.ChibiEntries.Select(c => Grp.GetFileByIndex(c.Animation)?.Name)
                            .Any(n => n?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
                    case ItemType.Group_Selection:
                    case ItemType.Scenario:
                        return "SCENARIOS".Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Item:
                        return ((ItemItem)item).ItemGraphic.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Layout:
                        return ((LayoutItem)item).Layout.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Map:
                        return ((MapItem)item).Map.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Place:
                        return ((PlaceItem)item).PlaceGraphic.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Puzzle:
                        return ((PuzzleItem)item).Puzzle.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Script:
                        return ((ScriptItem)item).Event.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.System_Texture:
                        return Grp.GetFileByIndex(((SystemTextureItem)item).SysTex.GrpIndex).Name.Contains(term, StringComparison.OrdinalIgnoreCase);
                    case ItemType.Topic:
                        return "TOPICS".Contains(term, StringComparison.OrdinalIgnoreCase);
                }
                return false;

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

            case SearchQuery.DataHolder.Command:
                if (item is ScriptItem commandScript)
                {
                    EventFile.CommandVerb command;
                    List<string> parameters = [];
                    int firstParen = term.IndexOf('(');
                    if (firstParen > 0)
                    {
                        command = Enum.Parse<EventFile.CommandVerb>(term[..firstParen]);
                        parameters.AddRange(term[(firstParen+1)..term.IndexOf(')')].Split(','));
                    }
                    else
                    {
                        command = Enum.Parse<EventFile.CommandVerb>(term);
                    }

                    return commandScript.GetScriptCommandTree(this, logger)
                        .Any(s => s.Value.Any(c => c.Verb == command &&
                                                   parameters.Count <= c.Parameters.Count &&
                                                   c.Parameters.Zip(parameters)
                                                       .All(z => string.IsNullOrWhiteSpace(z.Second) || z.Second == "*" ||
                                                                 (z.Second.StartsWith("!") && !(z.First?.GetValueString(this)?.Contains(z.Second[1..], StringComparison.OrdinalIgnoreCase) ?? false)) ||
                                                                 (z.First?.GetValueString(this)?.Contains(z.Second, StringComparison.OrdinalIgnoreCase) ?? false))));
                }
                return false;

            case SearchQuery.DataHolder.Flag:
                if (item is ScriptItem flagScript)
                {
                    return flagScript.GetScriptCommandTree(this, logger)
                        .Any(s => s.Value.Any(c => c.Parameters
                            .Where(p => p.Type == ScriptParameter.ParameterType.FLAG)
                            .Any(p => ((FlagScriptParameter)p).FlagName
                                .Contains(term, StringComparison.OrdinalIgnoreCase))));
                }
                else if (short.TryParse(term, out short flagTerm))
                {
                    if (item is BackgroundMusicItem flagBgm)
                    {
                        return flagBgm.Flag == flagTerm;
                    }
                    else if (item is BackgroundItem flagBg)
                    {
                        return flagBg.Flag == flagTerm;
                    }
                    else if (item is TopicItem flagTopic)
                    {
                        return flagTopic.TopicEntry.Id == flagTerm;
                    }
                    else if (item is PuzzleItem flagPuzzle)
                    {
                        return flagPuzzle.Puzzle.Settings.Unknown15 == flagTerm || flagPuzzle.Puzzle.Settings.Unknown16 == flagTerm;
                    }
                    else if (item is GroupSelectionItem flagGroupSelection)
                    {
                        return flagGroupSelection.Selection.Activities.Any(a => a?.Routes.Any(r => r?.Flag == flagTerm) ?? false);
                    }
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

            case SearchQuery.DataHolder.Orphaned_Items:
                if (IGNORED_ORPHAN_TYPES.Contains(item.Type)
                    // assume SFX that are in groups are referenced in code. william doesn't quite know what jonko means here but he trusts the process
                    || (item is SfxItem sfx && sfx.Name.Contains("SSE") && sfx.AssociatedGroups.Count > 0))
                {
                    return false;
                }
                return item.GetReferencesTo(this).Count == 0;

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

                    return references.Where(r => r.Type == ItemType.Script).Any() &&
                           references.Where(r => r.Type == ItemType.Script)
                               .All(r => ScriptIsInEpisode((ScriptItem)r, scenarioEpIndex, scenarioNextEpIndex));
                }
                else
                {
                    return references.Any(r => r.Type == ItemType.Script && ScriptIsInEpisode((ScriptItem)r, scenarioEpIndex, scenarioNextEpIndex));
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
            ItemDescription groupSelection = references.Find(r => r.Type == ItemType.Group_Selection);
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

    [GeneratedRegex(@"\((?<num>\d+)\)")]
    private static partial Regex ProjectNameAppendedNumber();
}
