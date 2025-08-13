using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using Moq;
using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Editors;
using SkiaSharp;

namespace SerialLoops.Tests.Headless.Editors;

[TestFixture]
public class CharacterEditorTests
{
    private UiVals _uiVals;
    private TestConsoleLogger _log;
    private MainWindowViewModel _mainWindowViewModel;
    private Project _project;
    private FontFile _fontFile;
    private MessageInfoFile _messInfo;
    private SKBitmap _speakerBitmap;
    private SKBitmap _nameplateBitmap;
    private GraphicInfo _nameplateInfo;
    private SKBitmap _fontBitmap;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _log = new();
        if (Path.Exists("ui_vals.json"))
        {
            _uiVals = JsonSerializer.Deserialize<UiVals>(await File.ReadAllTextAsync("ui_vals.json"));
        }
        else
        {
            _uiVals = await UiVals.DownloadTestAssets();
        }

        _messInfo = new();
        _messInfo.Initialize(await File.ReadAllBytesAsync(Path.Combine(_uiVals.AssetsDirectory, "MESSINFO.bin")), 0, _log);

        _fontFile = new();
        _fontFile.Initialize(await File.ReadAllBytesAsync(Path.Combine(_uiVals.AssetsDirectory, "FONT.bin")), 1, _log);

        GraphicsFile nameplateFile = new();
        nameplateFile.Initialize(await File.ReadAllBytesAsync(Path.Combine(_uiVals.AssetsDirectory, "SYS_CMN_B12.DNX")), 2, _log);
        _speakerBitmap = nameplateFile.GetImage(transparentIndex: 0);
        _nameplateBitmap = nameplateFile.GetImage();
        _nameplateInfo = new(nameplateFile);

        _fontBitmap = SKBitmap.Decode(Path.Combine(_uiVals.AssetsDirectory, "ZENFONT.png"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _speakerBitmap.Dispose();
        _nameplateBitmap.Dispose();
        _fontBitmap.Dispose();
        Directory.Delete(_mainWindowViewModel.CurrentConfig.UserDirectory, true);
    }

    [SetUp]
    public void Setup()
    {
        Mock<Project> projectMock = new();
        projectMock.SetupCommonMocks();
        _project = projectMock.Object;
        _project.SetupMockedProperties(_uiVals);
        _project.SpeakerBitmap = _speakerBitmap;
        _project.NameplateBitmap = _nameplateBitmap;
        _project.NameplateInfo = _nameplateInfo;
        _project.MessInfo = _messInfo;
        _project.FontMap = _fontFile;
        _project.FontBitmap = _fontBitmap;
        _project.Grp = new() { Files = [] };
        _project.Evt = new() { Files = [] };
        _project.Dat = new() { Files = [] };

        Mock<MainWindowViewModel> windowMock = new() { CallBase = true };
        windowMock.SetupCommonMocks();
        _mainWindowViewModel = windowMock.Object;
        _mainWindowViewModel.SetupMockedProperties(_project, _log);

        string testId = Guid.NewGuid().ToString();
        ConfigUser configUser = new() { UserDirectory = Path.Combine(Path.GetTempPath(), testId) };
        ConfigFactoryMock configFactory = new($"{testId}.json", configUser);
        _mainWindowViewModel.Window = new() { ConfigurationFactory = configFactory, DataContext = _mainWindowViewModel };
        _mainWindowViewModel.Window.Show();

        _project.ConfigUser = _mainWindowViewModel.CurrentConfig;
        _project.Name = nameof(CharacterEditorTests);
        Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, "assets", "graphics"));
        Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, "assets", "graphics"));
        Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, "assets", "data"));
        Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, "assets", "data"));

        SfxEntry[] sfxEntries = [new() { Index = 20, SequenceArchive = 0 }, new() { Index = 21, SequenceArchive = 0 }];
        _project.Snd = new();
        _project.Snd.SetupSndMock(sfxEntries, _log);

        string defaultCharactersJson = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultCharacters")}.en.json";
        _project.Characters = JsonSerializer.Deserialize<Dictionary<int, NameplateProperties>>(File.ReadAllText(defaultCharactersJson), Project.SERIALIZER_OPTIONS);

        _project.Items =
        [
            new CharacterItem(_messInfo.MessageInfos.First(m => m.Character == Speaker.KYON),
                new("Kyon", CharacterItem.BuiltInColors["Kyon"], CharacterItem.BuiltInColors["Kyon"], new(0x38, 0x38, 0x38), true),
                _project),
            new CharacterItem(_messInfo.MessageInfos.First(m => m.Character == Speaker.HARUHI),
                new("Haruhi", CharacterItem.BuiltInColors["Haruhi"], CharacterItem.BuiltInColors["Haruhi"], new(0x38, 0x38, 0x38), true),
                _project),
            new SfxItem(sfxEntries[0], "KyonSound", 20, _project),
            new SfxItem(sfxEntries[1], "HaruhiSound", 21, _project),
        ];
        _project.ItemNames = new()
        {
            { "CHR_Kyon", "CHR_Kyon" },
            { "CHR_Haruhi", "CHR_Haruhi" },
        };

        _project.Save(_log);
        _mainWindowViewModel.OpenProjectView(_project, new TestProgressTracker());
    }

    [AvaloniaTest]
    public void CharacterEditor_NameplateChangesPersistThroughReload()
    {
        int currentFrame = 0;

        string newName = "Booyah";
        SKColor newColor = new(0, 80, 255);
        SKColor newOutline = new(255, 0, 0);

        CharacterItem character = (CharacterItem)_project.Items[0];
        _mainWindowViewModel.EditorTabs.OpenTab(character);
        _mainWindowViewModel.Window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        Assert.That(_mainWindowViewModel.EditorTabs.SelectedTab.Description.DisplayName[4..], Is.Not.EqualTo(newName));

        CharacterEditorViewModel editor = (CharacterEditorViewModel)_mainWindowViewModel.EditorTabs.SelectedTab;
        Assert.Multiple(() =>
        {
            Assert.That(editor.TextColor, Is.Not.EqualTo(newColor));
            Assert.That(editor.PlateColor, Is.Not.EqualTo(newColor));
            Assert.That(editor.OutlineColor, Is.Not.EqualTo(newOutline));
        });
        editor.CharacterName = newName;
        _mainWindowViewModel.Window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        editor.TextColor = newColor;
        editor.PlateColor = newColor;
        editor.OutlineColor = newOutline;
        _mainWindowViewModel.Window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);

        if (OperatingSystem.IsMacOS())
        {
            _mainWindowViewModel.Window.KeyPress(Key.S, RawInputModifiers.Meta, PhysicalKey.S, "Cmd+S");
        }
        else
        {
            _mainWindowViewModel.Window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "Ctrl+S");
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_mainWindowViewModel.EditorTabs.SelectedTab.Description.DisplayName[4..], Is.EqualTo(newName));
            Assert.That(editor.TextColor, Is.EqualTo(newColor));
            Assert.That(editor.PlateColor, Is.EqualTo(newColor));
            Assert.That(editor.OutlineColor, Is.EqualTo(newOutline));
        }
        
        _mainWindowViewModel.CloseProjectCommand.Execute(null);
        _mainWindowViewModel.Window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        _mainWindowViewModel.OpenProject = _project;
        _mainWindowViewModel.OpenProjectView(_project, new TestProgressTracker());

        _mainWindowViewModel.EditorTabs.OpenTab(character);
        CharacterEditorViewModel newEditor = (CharacterEditorViewModel)_mainWindowViewModel.EditorTabs.SelectedTab;
        _mainWindowViewModel.Window.CaptureAndSaveFrame(Path.Combine(_uiVals.AssetsDirectory, "artifacts"), TestContext.CurrentContext.Test.Name, ref currentFrame);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_mainWindowViewModel.EditorTabs.SelectedTab.Description.DisplayName[4..], Is.EqualTo(newName));
            Assert.That(newEditor.TextColor, Is.EqualTo(newColor));
            Assert.That(newEditor.PlateColor, Is.EqualTo(newColor));
            Assert.That(newEditor.OutlineColor, Is.EqualTo(newOutline));
        }
    }
}
