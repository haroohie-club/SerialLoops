using HaruhiChokuretsuLib.Audio;
using NAudio.Wave;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SkiaSharp;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;

namespace SerialLoops.Tests
{
    public class CoreTests
    {
        private Project _project;
        private Config _config;
        private string _zipPath;
        private string _dataDir;

        private HaruhiChokuretsuLib.Util.ConsoleLogger _log;
        private ConsoleProgressTracker _progressTracker;

        [OneTimeSetUp]
        public void SetUp()
        {
            _zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "choku_data.zip");
            _dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "choku_data");

            _log = new();
            _progressTracker = new();

            // Download data archive
            _log.Log("Downloading data archive...");
            using HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, "https://haroohie.blob.core.windows.net/serial-loops/choku_data.zip");
            HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult().EnsureSuccessStatusCode(); // Setup methods can't be async so...
            File.WriteAllBytes(_zipPath, response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()); // i'm so sorry

            // Extract data archive
            _log.Log("Extracting data archive...");
            using FileStream zipFile = File.OpenRead(_zipPath);
            ZipArchive zip = new(zipFile);
            zip.ExtractToDirectory(_dataDir);

            // Create a project using the extracted data
            _log.Log("Creating project...");
            _config = Config.LoadConfig(_log);
            _project = new("Tester", "en", _config, _log);
            
            string archivesDir = Path.Combine("original", "archives");
            string bgmDir = Path.Combine("rom", "data", "bgm");
            string vceDir = Path.Combine("rom", "data", "vce");
            Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, archivesDir));
            Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, archivesDir));
            Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, bgmDir));
            Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, bgmDir));
            Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, vceDir));
            Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, vceDir));

            IO.CopyFiles(_dataDir, Path.Combine(_project.BaseDirectory, archivesDir));
            IO.CopyFiles(_dataDir, Path.Combine(_project.IterativeDirectory, archivesDir));
            IO.CopyFiles(Path.Combine(_dataDir, "bgm"), Path.Combine(_project.BaseDirectory, bgmDir));
            IO.CopyFiles(Path.Combine(_dataDir, "bgm"), Path.Combine(_project.IterativeDirectory, bgmDir));
            IO.CopyFiles(Path.Combine(_dataDir, "vce"), Path.Combine(_project.BaseDirectory, vceDir));
            IO.CopyFiles(Path.Combine(_dataDir, "vce"), Path.Combine(_project.IterativeDirectory, vceDir));

            // Load the project archives
            _project.LoadArchives(_log, _progressTracker);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            File.Delete(_zipPath);
            Directory.Delete(_dataDir, true);
            Directory.Delete(_project.MainDirectory, true);
        }

        #region Item Tests
        [Test, TestCaseSource(nameof(BackgroundNames)), Parallelizable(ParallelScope.All)]
        public void BackgroundTest(string bgName)
        {
            var bg = (BackgroundItem)_project.Items.First(i => i.Name == bgName);
            Assert.Multiple(() =>
            {
                Assert.That(bg.Graphic1, Is.Not.Null);
                SKBitmap preview = bg.GetPreview(_project);
                Assert.That(preview, Is.Not.Null);
            });
        }

        [Test, TestCaseSource(nameof(BgmNames)), Parallelizable(ParallelScope.All)]
        public void BackgroundMusicItemTest(string bgmName)
        {
            var bgm = (BackgroundMusicItem)_project.Items.First(i => i.Name == bgmName);
            Assert.Multiple(() =>
            {
                Assert.That(bgm.BgmFile, Does.Contain(bgm.Name));
                Assert.That(bgm.GetWaveProvider(_log, true), Is.Not.Null);
            });
        }

        [Test, TestCaseSource(nameof(CharacterSpriteNames)), Parallelizable(ParallelScope.All)]
        public void CharacterSpriteItemTest(string spriteName)
        {
            var sprite = (CharacterSpriteItem)_project.FindItem(spriteName);
            var closeMouthAnimation = sprite.GetClosedMouthAnimation(_project);
            var lipFlapAnimation = sprite.GetLipFlapAnimation(_project);
            Assert.Multiple(() =>
            {
                Assert.That(closeMouthAnimation, Is.Not.Null);
                Assert.That(lipFlapAnimation, Is.Not.Null);
            });
        }

        [Test, TestCaseSource(nameof(ChibiNames)), Parallelizable(ParallelScope.All)]
        public void ChibiItemTest(string chibiName)
        {
            var chibi = (ChibiItem)_project.Items.First(i => i.Name == chibiName);
            Assert.Multiple(() =>
            {
                Assert.That(chibi.Chibi, Is.Not.Null);
                Assert.That(chibi.ChibiEntries, Is.Not.Empty);
                Assert.That(chibi.ChibiAnimations, Is.Not.Empty);
            });
        }

        [Test, TestCaseSource(nameof(GroupSelectionNames)), Parallelizable(ParallelScope.All)]
        public void GroupSelectionItemTest(string groupSelectionName)
        {
            var groupSelection = (GroupSelectionItem)_project.FindItem(groupSelectionName);
            Assert.That(groupSelection.Selection.RouteSelections, Is.Not.Empty);
        }

        [Test, TestCaseSource(nameof(MapNames)), Parallelizable(ParallelScope.All)]
        public void MapItemTest(string mapNames)
        {
            Assert.Pass();
        }

        [Test, TestCaseSource(nameof(PuzzleNames)), Parallelizable(ParallelScope.All)]
        public void PuzzleItemTest(string puzzleName)
        {
            var puzzle = (PuzzleItem)_project.FindItem(puzzleName);
            Assert.That(puzzle.SingularityImage, Is.Not.Null);
        }

        [Test, TestCaseSource(nameof(ScriptNames)), Parallelizable(ParallelScope.All)]
        public void ScriptItemTest(string scriptName)
        {
            var script = (ScriptItem)_project.FindItem(scriptName);

            var commandTree = script.GetScriptCommandTree(_project, _log);
            script.CalculateGraphEdges(commandTree, _log);
            Assert.That(script.Graph, Is.Not.Null);
            foreach (ScriptItemCommand command in commandTree.Values.SelectMany(c => c))
            {
                if (scriptName == "EV1_001" && command.Section.Name == "NONEHL001")
                {
                    // There is one section in one script that is truly unused
                    continue;
                }
                Assert.That(command.WalkCommandGraph(commandTree, script.Graph), Is.Not.Null.And.Not.Empty, $"Failed on command {command.Verb} ({command.Index} in {command.Section.Name})");
            }
        }

        [Test, TestCaseSource(nameof(VoiceNames)), Parallelizable(ParallelScope.All)]
        public void VoiceItemTest(string voiceName)
        {
            var vce = (VoicedLineItem)_project.FindItem(voiceName);
            Assert.Multiple(() =>
            {
                Assert.That(vce.VoiceFile, Does.Contain(vce.Name));
                Assert.That(vce.GetWaveProvider(_log), Is.Not.Null);
            }); 
        }
        #endregion

        #region Form Functionality Tests
        [Test, Parallelizable]
        public void ReferencesTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_project.Items.First(i => i.Name == "BG_BG_AKID0").GetReferencesTo(_project), Has.Count.EqualTo(14));
                Assert.That(_project.Items.First(i => i.Name == "BGM001").GetReferencesTo(_project), Has.Count.EqualTo(9));
                Assert.That(_project.Items.First(i => i.Name == "SPR_CLUB_MEM_A_234").GetReferencesTo(_project), Has.Count.EqualTo(12));
                Assert.That(_project.Items.First(i => i.Name == "CHIBI_HAL").GetReferencesTo(_project), Has.Count.EqualTo(59));
                Assert.That(_project.Items.First(i => i.Name == "SLG01").GetReferencesTo(_project), Has.Count.EqualTo(1));
                Assert.That(_project.Items.First(i => i.Name == "CA01").GetReferencesTo(_project), Has.Count.EqualTo(1));
                Assert.That(_project.Items.First(i => i.Name == "EV0_000").GetReferencesTo(_project), Has.Count.EqualTo(1));
                Assert.That(_project.Items.First(i => i.Name == "122").GetReferencesTo(_project), Has.Count.EqualTo(5));
                Assert.That(_project.Items.First(i => i.Name == "ANZ_ANOTHER00").GetReferencesTo(_project), Has.Count.EqualTo(2));
            });
        }

        [Test, Parallelizable]
        public void SearchTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_project.GetSearchResults("EV1_"), Has.Count.EqualTo(30), "Failed on EV1_");
                Assert.That(_project.GetSearchResults("EV2_"), Has.Count.EqualTo(32), "Failed on EV2_");
                Assert.That(_project.GetSearchResults("EV3_"), Has.Count.EqualTo(36), "Failed on EV3_");
                Assert.That(_project.GetSearchResults("EV4_"), Has.Count.EqualTo(43), "Failed on EV4_");
                Assert.That(_project.GetSearchResults("EV5_"), Has.Count.EqualTo(36), "Failed on EV5_");
                Assert.That(_project.GetSearchResults("ANZ"), Has.Count.EqualTo(26), "Failed on ANZ");
                Assert.That(_project.GetSearchResults("BG_"), Has.Count.EqualTo(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background).Count()), "Failed on BG_");
                Assert.That(_project.GetSearchResults("BGM0"), Has.Count.EqualTo(_project.Items.Where(i => i.Type == ItemDescription.ItemType.BGM).Count()), "Failed on BGM");
                Assert.That(_project.GetSearchResults("SLG"), Has.Count.EqualTo(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Puzzle).Count()), "Failed on SLG");
                Assert.That(_project.GetSearchResults("SPR_"), Has.Count.EqualTo(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Count()), "Failed on SPR_");
            });
        }
        #endregion

        #region Test Population Functions
        private static string[] BackgroundNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Backgrounds.txt"));
        }
        private static string[] BgmNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "BGMs.txt"));
        }
        private static string[] CharacterSpriteNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Character_Sprites.txt"));
        }
        private static string[] ChibiNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Chibis.txt"));
        }
        public static string[] GroupSelectionNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Group_Selections.txt"));
        }
        private static string[] MapNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Maps.txt"));
        }
        private static string[] PuzzleNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Puzzles.txt"));
        }
        private static string[] ScriptNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Scripts.txt"));
        }
        private static string[] VoiceNames()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "item_names", "Voices.txt"));
        }
        #endregion
    }
}
