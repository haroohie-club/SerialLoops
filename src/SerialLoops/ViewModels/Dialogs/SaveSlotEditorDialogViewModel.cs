using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.SaveFile;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Panels;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs;

public class SaveSlotEditorDialogViewModel : ViewModelBase
{
    private SaveItem _save;
    private CommonSaveData _commonSaveData;
    private SaveSlotData _saveSlot;
    private QuickSaveSlotData _quickSave;

    public string Title { get; }
    public SaveSection SaveSection { get; }
    public string SlotName { get; }
    private Project _project;
    private EditorTabsPanelViewModel _tabs;

    public bool IsCommonSave { get; } = false;
    public bool IsSaveSlot { get; } = false;
    public bool IsQuickSave { get; } = false;

    public SaveSlotEditorDialogViewModel(SaveItem save, SaveSection saveSection, string saveName, string slotName, Project project,
        ILogger log, EditorTabsPanelViewModel tabs = null)
    {
        _save = save;
        Title = string.Format(Strings.Edit_Save_File____0_____1_, saveName, slotName);
        SaveSection = saveSection;
        SlotName = slotName;
        _project = project;
        _tabs = tabs;

        if (SaveSection is QuickSaveSlotData quickSave)
        {
            IsSaveSlot = true;
            IsQuickSave = true;
            _quickSave = quickSave;
        }
        else if (SaveSection is SaveSlotData saveSlot)
        {
            IsSaveSlot = true;
            _saveSlot = saveSlot;
        }
        else if (SaveSection is CommonSaveData commonSave)
        {
            IsCommonSave = true;
            _commonSaveData = commonSave;
            CharacterItem kyon = _project.GetCharacterBySpeaker(Speaker.KYON);
            KyonVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, kyon);
            KyonName = kyon.DisplayName[4..];
            CharacterItem haruhi = _project.GetCharacterBySpeaker(Speaker.HARUHI);
            HaruhiVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, haruhi);
            HaruhiName = haruhi.DisplayName[4..];
            CharacterItem asahina = _project.GetCharacterBySpeaker(Speaker.MIKURU);
            AsahinaVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, asahina);
            AsahinaName = asahina.DisplayName[4..];
            CharacterItem nagato = _project.GetCharacterBySpeaker(Speaker.NAGATO);
            NagatoVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, nagato);
            NagatoName = nagato.DisplayName[4..];
            CharacterItem koizumi = _project.GetCharacterBySpeaker(Speaker.KOIZUMI);
            KoizumiVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, koizumi);
            KoizumiName = koizumi.DisplayName[4..];

            CharacterItem sis = _project.GetCharacterBySpeaker(Speaker.KYON_SIS);
            SisterVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, sis);
            SisterName = sis.DisplayName[4..];
            CharacterItem tsuruya = _project.GetCharacterBySpeaker(Speaker.TSURUYA);
            TsuruyaVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, tsuruya);
            TsuruyaName = tsuruya.DisplayName[4..];
            CharacterItem taniguchi = _project.GetCharacterBySpeaker(Speaker.TANIGUCHI);
            TaniguchiVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, taniguchi);
            TaniguchiName = taniguchi.DisplayName[4..];
            CharacterItem kunikida = _project.GetCharacterBySpeaker(Speaker.KUNIKIDA);
            KunikidaVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, kunikida);
            KunikidaName = kunikida.DisplayName[4..];
            CharacterItem mysteryGirl = _project.GetCharacterBySpeaker(Speaker.GIRL);
            MysteryGirlVoicePortrait = Shared.GetCharacterVoicePortrait(_project, log, mysteryGirl);
            MysteryGirlName = mysteryGirl.DisplayName[4..];

            _numSaves = commonSave.NumSaves;
            _bgmVolume = commonSave.Options.BgmVolume;
            _sfxVolume = commonSave.Options.SfxVolume;
            _wordsVolume = commonSave.Options.WordsVolume;
            _voiceVolume = commonSave.Options.VoiceVolume;
        }
    }

    // Common Save Data
    private int _numSaves;
    public int NumSaves
    {
        get => _numSaves;
        set
        {
            this.RaiseAndSetIfChanged(ref _numSaves, value);
            _commonSaveData.NumSaves = _numSaves;
            _save.UnsavedChanges = true;
        }
    }

    private int _bgmVolume;
    public int BgmVolume
    {
        get => _bgmVolume / 10;
        set
        {
            this.RaiseAndSetIfChanged(ref _bgmVolume, value * 10);
            _commonSaveData.Options.BgmVolume = _bgmVolume;
            _save.UnsavedChanges = true;
        }
    }

    private int _sfxVolume;
    public int SfxVolume
    {
        get => _sfxVolume / 10;
        set
        {
            this.RaiseAndSetIfChanged(ref _sfxVolume, value * 10);
            _commonSaveData.Options.SfxVolume = _sfxVolume;
            _save.UnsavedChanges = true;
        }
    }

    private int _wordsVolume;
    public int WordsVolume
    {
        get => _wordsVolume / 10;
        set
        {
            this.RaiseAndSetIfChanged(ref _wordsVolume, value * 10);
            _commonSaveData.Options.WordsVolume = _wordsVolume;
            _save.UnsavedChanges = true;
        }
    }

    private int _voiceVolume;
    public int VoiceVolume
    {
        get => _voiceVolume / 10;
        set
        {
            this.RaiseAndSetIfChanged(ref _voiceVolume, value * 10);
            _commonSaveData.Options.VoiceVolume = _voiceVolume;
            _save.UnsavedChanges = true;
        }
    }

    public SKBitmap KyonVoicePortrait { get; }
    public string KyonName { get; }
    public SKBitmap HaruhiVoicePortrait { get; }
    public string HaruhiName { get; }
    public SKBitmap AsahinaVoicePortrait { get; }
    public string AsahinaName { get; }
    public SKBitmap NagatoVoicePortrait { get; }
    public string NagatoName { get; }
    public SKBitmap KoizumiVoicePortrait { get; }
    public string KoizumiName { get; }
    public SKBitmap SisterVoicePortrait { get; }
    public string SisterName { get; }
    public SKBitmap TsuruyaVoicePortrait { get; }
    public string TsuruyaName { get; }
    public SKBitmap TaniguchiVoicePortrait { get; }
    public string TaniguchiName { get; }
    public SKBitmap KunikidaVoicePortrait { get; }
    public string KunikidaName { get; }
    public SKBitmap MysteryGirlVoicePortrait { get; }
    public string MysteryGirlName { get; }
}
