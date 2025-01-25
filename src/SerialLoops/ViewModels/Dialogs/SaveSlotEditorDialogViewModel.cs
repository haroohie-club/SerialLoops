using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AvaloniaEdit.Utils;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.SaveFile;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using SoftCircuits.Collections;

namespace SerialLoops.ViewModels.Dialogs;

public class SaveSlotEditorDialogViewModel : ViewModelBase
{
    private SaveItem _save;
    private CommonSaveData _commonSaveData;
    private SaveSlotData _saveSlot;
    private QuickSaveSlotData _quickSave;
    private ILogger _log;

    public string Title { get; }
    public SaveSection SaveSection { get; }
    public string SlotName { get; }
    private Project _project;
    public EditorTabsPanelViewModel Tabs { get; }

    public bool IsCommonSave { get; }
    public bool IsSaveSlot { get; }
    public bool IsQuickSave { get; }

    [Reactive]
    public bool HideUnsetFlags { get; set; }
    [Reactive]
    public string FlagFilter { get; set; }
    [Reactive]
    public string ResultsLabel { get; set; }

    public ICommand FilterFlagsCommand { get; }
    public ICommand PreviousFlagsCommand { get; }
    public ICommand NextFlagsCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public SaveSlotEditorDialogViewModel(SaveItem save, SaveSection saveSection, string saveName, string slotName, Project project,
        ILogger log, EditorTabsPanelViewModel tabs = null)
    {
        _save = save;
        _log = log;
        Title = string.Format(Strings.Edit_Save_File____0_____1_, saveName, slotName);
        SaveSection = saveSection;
        SlotName = slotName;
        _project = project;
        Tabs = tabs;

        if (SaveSection is QuickSaveSlotData quickSave)
        {
            IsQuickSave = true;
            _quickSave = quickSave;
            ScriptItems = new(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Cast<ScriptItem>());
            _selectedScriptItem = ScriptItems.FirstOrDefault(i => i.Event.Index == _quickSave.CurrentScript);
            if (_selectedScriptItem is not null)
            {
                _currentCommandTree = _selectedScriptItem.GetScriptCommandTree(_project, _log);
                _selectedScriptItem.CalculateGraphEdges(_currentCommandTree, _log);
                ScriptSections = new(_selectedScriptItem.Event.ScriptSections);
                SelectedScriptSection = ScriptSections[_quickSave.CurrentScriptBlock];
                _scriptCommandIndex = _quickSave.CurrentScriptCommand;
            }

            List<(ChibiItem Chibi, int X, int Y)> topScreenChibis = [];
            int chibiCurrentX = 80;
            const int chibiY = 100;
            for (int i = 1; i <= 5; i++)
            {
                if (!_quickSave.TopScreenChibis.HasFlag((CharacterMask)(1 << i)))
                {
                    continue;
                }

                ChibiItem chibi = (ChibiItem)_project.Items.First(it => it.Type == ItemDescription.ItemType.Chibi && ((ChibiItem)it).TopScreenIndex == i);
                topScreenChibis.Add((chibi, chibiCurrentX, chibiY));
                chibiCurrentX += chibi.ChibiAnimations.First().Value.ElementAt(0).Frame.Width - 16;
            }

            _scriptPreview = new()
            {
                Background = (BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == (_quickSave.CgIndex != 0 ? _quickSave.CgIndex : _quickSave.BgIndex)),
                BgPalEffect = (PaletteEffectScriptParameter.PaletteEffect)_quickSave.BgPalEffect,
                EpisodeHeader = _quickSave.EpisodeHeader,
                Kbg = (BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == _quickSave.KbgIndex),
                Place = (PlaceItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Place && ((PlaceItem)i).Index == _quickSave.Place),
                TopScreenChibis = topScreenChibis,
                Sprites =
                [
                    new()
                    {
                        Sprite = (CharacterSpriteItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == _quickSave.FirstCharacterSprite),
                        Positioning = new() { X = _quickSave.Sprite1XOffset, Layer = 2 },
                    },
                    new()
                    {
                        Sprite = (CharacterSpriteItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == _quickSave.SecondCharacterSprite),
                        Positioning = new() { X = _quickSave.Sprite2XOffset, Layer = 1 },
                    },
                    new()
                    {
                        Sprite = (CharacterSpriteItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == _quickSave.ThirdCharacterSprite),
                        Positioning = new() { X = _quickSave.Sprite3XOffset, Layer = 0 },
                    },
                ],
            };

            (ScriptPreview, ErrorImagePath) = ScriptItem.GeneratePreviewImage(_scriptPreview, _project);
        }
        if (SaveSection is SaveSlotData saveSlot)
        {
            IsSaveSlot = true;
            _saveSlot = saveSlot;
            LoadCharacterPortraits();

            SaveDate = _saveSlot.SaveTime.Date;
            SaveTime = _saveSlot.SaveTime.TimeOfDay;
            ScenarioCommandPickerVm = new(_saveSlot.ScenarioPosition, (ScenarioItem)_project.Items.First(s => s.Type == ItemDescription.ItemType.Scenario));
            Episode = _saveSlot.EpisodeNumber;
            HaruhiFriendshipLevel = _saveSlot.HaruhiFriendshipLevel;
            AsahinaFriendshipLevel = _saveSlot.MikuruFriendshipLevel;
            NagatoFriendshipLevel = _saveSlot.NagatoFriendshipLevel;
            KoizumiFriendshipLevel = _saveSlot.KoizumiFriendshipLevel;
            TsuruyaFriendshipLevel = _saveSlot.TsuruyaFriendshipLevel;
            UnknownFriendshipLevel = _saveSlot.UnknownFriendshipLevel;
            RecentObjectives = [new("A", _saveSlot.ObjectiveA), new("B", _saveSlot.ObjectiveB), new("C", _saveSlot.ObjectiveC), new("D", _saveSlot.ObjectiveD)];
        }
        else if (SaveSection is CommonSaveData commonSave)
        {
            IsCommonSave = true;
            _commonSaveData = commonSave;
            LoadCharacterPortraits();

            NumSaves = commonSave.NumSaves;
            BgmVolume = commonSave.Options.BgmVolume / 10;
            SfxVolume = commonSave.Options.SfxVolume / 10;
            WordsVolume = commonSave.Options.WordsVolume / 10;
            VoiceVolume = commonSave.Options.VoiceVolume / 10;

            KyonVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.KYON) != 0;
            HaruhiVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.HARUHI) != 0;
            AsahinaVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.MIKURU) != 0;
            NagatoVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.NAGATO) != 0;
            KoizumiVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.KOIZUMI) != 0;
            SisterVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.SISTER) != 0;
            TsuruyaVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.TSURUYA) != 0;
            TaniguchiVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.TANIGUCHI) != 0;
            KunikidaVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.KUNIKIDA) != 0;
            MysteryGirlVoiceEnabled = (commonSave.Options.VoiceToggles & SaveOptions.VoiceOptions.MYSTERY_GIRL) != 0;

            PuzzleInterruptScenesOff = (commonSave.Options.StoryOptions &
                                         SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPTE_SCENES_ON) == 0 &&
                                        (commonSave.Options.StoryOptions & SaveOptions.PuzzleInvestigationOptions
                                            .PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY) == 0;
            PuzzleInterruptScenesUnseenOnly = (commonSave.Options.StoryOptions &
                                               SaveOptions.PuzzleInvestigationOptions
                                                   .PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY) != 0;
            PuzzleInterruptScenesOn = (commonSave.Options.StoryOptions &
                                       SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPTE_SCENES_ON) != 0;
            TopicStockMode =
                (commonSave.Options.StoryOptions & SaveOptions.PuzzleInvestigationOptions.TOPIC_STOCK_MODE_ON) != 0;
            DialogueSkipping = (commonSave.Options.StoryOptions &
                                SaveOptions.PuzzleInvestigationOptions.DIALOGUE_SKIPPING_SKIP_ALREADY_READ) != 0;
            BatchDialogueDisplay = (commonSave.Options.StoryOptions &
                                    SaveOptions.PuzzleInvestigationOptions.BATCH_DIALOGUE_DISPLAY_ON) != 0;

            PowerStatuses =
            [
                new(project.GetCharacterBySpeaker(Speaker.MIKURU).DisplayName[4..], commonSave.MikuruPowerStatus),
                new(project.GetCharacterBySpeaker(Speaker.NAGATO).DisplayName[4..], commonSave.NagatoPowerStatus),
                new(project.GetCharacterBySpeaker(Speaker.KOIZUMI).DisplayName[4..], commonSave.KoizumiPowerStatus),
            ];
        }

        _flags = new LocalizedFlag[Flags.NUM_FLAGS];
        for (int i = 0; i < _flags.Length; i++)
        {
            _flags[i] = new(i, project, saveSection.IsFlagSet(i));
        }
        _filteredFlags = _flags;
        VisibleFlags.AddRange(_filteredFlags[..12]);

        FilterFlagsCommand = ReactiveCommand.Create(FilterFlags);
        PreviousFlagsCommand = ReactiveCommand.Create(() =>
        {
            if (FlagPage > 1)
            {
                FlagPage--;
            }
        });
        NextFlagsCommand = ReactiveCommand.Create(() =>
        {
            if (FlagPage < NumPages)
            {
                FlagPage++;
            }
        });
        SaveCommand = ReactiveCommand.Create<SaveSlotEditorDialog>(Save);
        CancelCommand = ReactiveCommand.Create<SaveSlotEditorDialog>(dialog => dialog.Close());
    }

    private void Save(SaveSlotEditorDialog dialog)
    {
        _save.UnsavedChanges = true;

        if (IsCommonSave)
        {
            _commonSaveData.NumSaves = NumSaves;
            _commonSaveData.Options.BgmVolume = BgmVolume * 10;
            _commonSaveData.Options.SfxVolume = SfxVolume * 10;
            _commonSaveData.Options.WordsVolume = WordsVolume * 10;
            _commonSaveData.Options.VoiceVolume = VoiceVolume * 10;

            _commonSaveData.Options.VoiceToggles = 0;
            if (KyonVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.KYON;
            }
            if (HaruhiVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.HARUHI;
            }
            if (AsahinaVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.MIKURU;
            }
            if (NagatoVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.NAGATO;
            }
            if (KoizumiVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.KOIZUMI;
            }
            if (SisterVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.SISTER;
            }
            if (TsuruyaVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.TSURUYA;
            }
            if (TaniguchiVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.TANIGUCHI;
            }
            if (KunikidaVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.KUNIKIDA;
            }
            if (MysteryGirlVoiceEnabled)
            {
                _commonSaveData.Options.VoiceToggles |= SaveOptions.VoiceOptions.MYSTERY_GIRL;
            }

            _commonSaveData.Options.StoryOptions = 0;
            if (PuzzleInterruptScenesOn)
            {
                _commonSaveData.Options.StoryOptions |=
                    SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPTE_SCENES_ON;
            }
            else if (PuzzleInterruptScenesUnseenOnly)
            {
                _commonSaveData.Options.StoryOptions |=
                    SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY;
            }
            if (TopicStockMode)
            {
                _commonSaveData.Options.StoryOptions |= SaveOptions.PuzzleInvestigationOptions.TOPIC_STOCK_MODE_ON;
            }
            if (DialogueSkipping)
            {
                _commonSaveData.Options.StoryOptions |=
                    SaveOptions.PuzzleInvestigationOptions.DIALOGUE_SKIPPING_SKIP_ALREADY_READ;
            }
            if (BatchDialogueDisplay)
            {
                _commonSaveData.Options.StoryOptions |=
                    SaveOptions.PuzzleInvestigationOptions.BATCH_DIALOGUE_DISPLAY_ON;
            }

            PowerStatuses[0].SetStatus(_commonSaveData.MikuruPowerStatus);
            PowerStatuses[1].SetStatus(_commonSaveData.NagatoPowerStatus);
            PowerStatuses[2].SetStatus(_commonSaveData.KoizumiPowerStatus);

            foreach (LocalizedFlag flag in _flags)
            {
                if (flag.IsSet)
                {
                    _commonSaveData.SetFlag(flag.Id);
                }
                else
                {
                    _commonSaveData.ClearFlag(flag.Id);
                }
            }
        }

        if (IsSaveSlot)
        {
            _saveSlot.SaveTime = new(DateOnly.FromDateTime(SaveDate), TimeOnly.FromTimeSpan(SaveTime), TimeSpan.Zero);
            _saveSlot.ScenarioPosition = ScenarioCommandPickerVm.ScenarioCommandIndex;
            _saveSlot.EpisodeNumber = Episode;
            _saveSlot.ObjectiveA = RecentObjectives[0].GetCharacterMask();
            _saveSlot.ObjectiveB = RecentObjectives[1].GetCharacterMask();
            _saveSlot.ObjectiveC = RecentObjectives[2].GetCharacterMask();
            _saveSlot.ObjectiveD = RecentObjectives[3].GetCharacterMask();
            for (int i = RecentObjectives.Count - 1; i >= 0; i--)
            {
                if (RecentObjectives[i].KyonPresent)
                {
                    _saveSlot.KyonObjectiveIndex = i;
                    break;
                }
            }
            _saveSlot.HaruhiFriendshipLevel = HaruhiFriendshipLevel;
            _saveSlot.MikuruFriendshipLevel = AsahinaFriendshipLevel;
            _saveSlot.NagatoFriendshipLevel = NagatoFriendshipLevel;
            _saveSlot.KoizumiFriendshipLevel = KoizumiFriendshipLevel;
            _saveSlot.TsuruyaFriendshipLevel = TsuruyaFriendshipLevel;
            _saveSlot.UnknownFriendshipLevel = UnknownFriendshipLevel;

            foreach (LocalizedFlag flag in _flags)
            {
                if (flag.IsSet)
                {
                    _saveSlot.SetFlag(flag.Id);
                }
                else
                {
                    _saveSlot.ClearFlag(flag.Id);
                }
            }
        }

        if (IsQuickSave)
        {
            _quickSave.CurrentScript = SelectedScriptItem.Event.Index;
            _quickSave.CurrentScriptBlock = SelectedScriptItem.Event.ScriptSections.IndexOf(SelectedScriptSection);
            _quickSave.CurrentScriptCommand = ScriptCommandIndex;
            _quickSave.KbgIndex = (short)(_scriptPreview.Kbg?.Id ?? 0);
            _quickSave.Place = (short)(_scriptPreview.Place?.Index ?? 0);
            if (_scriptPreview.Background.BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_BG)
            {
                _quickSave.BgIndex = (short)_scriptPreview.Background.Id;
            }
            else
            {
                OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commandTree = SelectedScriptItem.GetScriptCommandTree(_project, _log);
                ScriptItemCommand currentCommand = commandTree[SelectedScriptItem.Event.ScriptSections[_quickSave.CurrentScriptBlock]][ScriptCommandIndex];
                List<ScriptItemCommand> commands = currentCommand.WalkCommandGraph(commandTree, SelectedScriptItem.Graph);
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    if (commands[i].Verb == EventFile.CommandVerb.BG_DISP || commands[i].Verb == EventFile.CommandVerb.BG_DISP2 || (commands[i].Verb == EventFile.CommandVerb.BG_FADE && (((BgScriptParameter)commands[i].Parameters[0]).Background is not null)))
                    {
                        _quickSave.BgIndex = (short)((BgScriptParameter)commands[i].Parameters[0]).Background.Id;
                    }
                }
                _quickSave.CgIndex = (short)_scriptPreview.Background.Id;
            }
            _quickSave.BgPalEffect = (short)_scriptPreview.BgPalEffect;
            _quickSave.EpisodeHeader = _scriptPreview.EpisodeHeader;
            for (int i = 1; i <= 5; i++)
            {
                if (_scriptPreview.TopScreenChibis.Any(c => c.Chibi.TopScreenIndex == i))
                {
                    _quickSave.TopScreenChibis |= (CharacterMask)(1 << i);
                }
            }
            _quickSave.FirstCharacterSprite = _scriptPreview.Sprites.ElementAtOrDefault(0).Sprite?.Index ?? 0;
            _quickSave.SecondCharacterSprite = _scriptPreview.Sprites.ElementAtOrDefault(1).Sprite?.Index ?? 0;
            _quickSave.ThirdCharacterSprite = _scriptPreview.Sprites.ElementAtOrDefault(2).Sprite?.Index ?? 0;
            _quickSave.Sprite1XOffset = (short)(_scriptPreview.Sprites.ElementAtOrDefault(0).Positioning?.X ?? 0);
            _quickSave.Sprite2XOffset = (short)(_scriptPreview.Sprites.ElementAtOrDefault(1).Positioning?.X ?? 0);
            _quickSave.Sprite3XOffset = (short)(_scriptPreview.Sprites.ElementAtOrDefault(2).Positioning?.X ?? 0);
        }

        dialog.Close();
    }

    private void FilterFlags()
    {
        if (string.IsNullOrWhiteSpace(FlagFilter))
        {
            FlagFilter = "";
        }
        _filteredFlags = [.. HideUnsetFlags
            ? _flags.Where(f => f.IsSet && f.Description.Contains(FlagFilter, StringComparison.OrdinalIgnoreCase))
            : _flags.Where(f => f.Description.Contains(FlagFilter.Trim(), StringComparison.OrdinalIgnoreCase))];
        NumPages = _filteredFlags.Length / 12 + (_filteredFlags.Length % 12 == 0 ? 0 : 1);
        FlagPage = 1;
    }

    public void LoadCharacterPortraits()
    {
        CharacterItem kyon = _project.GetCharacterBySpeaker(Speaker.KYON);
        KyonVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, kyon);
        KyonName = kyon.DisplayName[4..];
        CharacterItem haruhi = _project.GetCharacterBySpeaker(Speaker.HARUHI);
        HaruhiVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, haruhi);
        HaruhiName = haruhi.DisplayName[4..];
        CharacterItem asahina = _project.GetCharacterBySpeaker(Speaker.MIKURU);
        AsahinaVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, asahina);
        AsahinaName = asahina.DisplayName[4..];
        CharacterItem nagato = _project.GetCharacterBySpeaker(Speaker.NAGATO);
        NagatoVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, nagato);
        NagatoName = nagato.DisplayName[4..];
        CharacterItem koizumi = _project.GetCharacterBySpeaker(Speaker.KOIZUMI);
        KoizumiVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, koizumi);
        KoizumiName = koizumi.DisplayName[4..];

        CharacterItem sis = _project.GetCharacterBySpeaker(Speaker.KYON_SIS);
        SisterVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, sis);
        SisterName = sis.DisplayName[4..];
        CharacterItem tsuruya = _project.GetCharacterBySpeaker(Speaker.TSURUYA);
        TsuruyaVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, tsuruya);
        TsuruyaName = tsuruya.DisplayName[4..];
        CharacterItem taniguchi = _project.GetCharacterBySpeaker(Speaker.TANIGUCHI);
        TaniguchiVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, taniguchi);
        TaniguchiName = taniguchi.DisplayName[4..];
        CharacterItem kunikida = _project.GetCharacterBySpeaker(Speaker.KUNIKIDA);
        KunikidaVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, kunikida);
        KunikidaName = kunikida.DisplayName[4..];
        CharacterItem mysteryGirl = _project.GetCharacterBySpeaker(Speaker.GIRL);
        MysteryGirlVoicePortrait = Shared.GetCharacterVoicePortrait(_project, _log, mysteryGirl);
        MysteryGirlName = mysteryGirl.DisplayName[4..];
    }

    // Common Save Data
    [Reactive]
    public int NumSaves { get; set; }

    [Reactive]
    public int BgmVolume { get; set; }
    [Reactive]
    public int SfxVolume { get; set; }
    [Reactive]
    public int WordsVolume { get; set; }
    [Reactive]
    public int VoiceVolume { get; set; }

    [Reactive] public SKBitmap KyonVoicePortrait { get; private set; }
    [Reactive] public string KyonName { get; private set; }
    [Reactive] public SKBitmap HaruhiVoicePortrait { get; private set; }
    [Reactive] public string HaruhiName { get; private set; }
    [Reactive] public SKBitmap AsahinaVoicePortrait { get; private set; }
    [Reactive] public string AsahinaName { get; private set; }
    [Reactive] public SKBitmap NagatoVoicePortrait { get; private set; }
    [Reactive] public string NagatoName { get; private set; }
    [Reactive] public SKBitmap KoizumiVoicePortrait { get; private set; }
    [Reactive] public string KoizumiName { get; private set; }
    [Reactive] public SKBitmap SisterVoicePortrait { get; private set; }
    [Reactive] public string SisterName { get; private set; }
    [Reactive] public SKBitmap TsuruyaVoicePortrait { get; private set; }
    [Reactive] public string TsuruyaName { get; private set; }
    [Reactive] public SKBitmap TaniguchiVoicePortrait { get; private set; }
    [Reactive] public string TaniguchiName { get; private set; }
    [Reactive] public SKBitmap KunikidaVoicePortrait { get; private set; }
    [Reactive] public string KunikidaName { get; private set; }
    [Reactive] public SKBitmap MysteryGirlVoicePortrait { get; private set; }
    [Reactive] public string MysteryGirlName { get; private set; }

    [Reactive]
    public bool KyonVoiceEnabled { get; set; }
    [Reactive]
    public bool HaruhiVoiceEnabled { get; set; }
    [Reactive]
    public bool AsahinaVoiceEnabled { get; set; }
    [Reactive]
    public bool NagatoVoiceEnabled { get; set; }
    [Reactive]
    public bool KoizumiVoiceEnabled { get; set; }
    [Reactive]
    public bool SisterVoiceEnabled { get; set; }
    [Reactive]
    public bool TsuruyaVoiceEnabled { get; set; }
    [Reactive]
    public bool TaniguchiVoiceEnabled { get; set; }
    [Reactive]
    public bool KunikidaVoiceEnabled { get; set; }
    [Reactive]
    public bool MysteryGirlVoiceEnabled { get; set; }

    [Reactive]
    public bool PuzzleInterruptScenesOff { get; set; }
    [Reactive]
    public bool PuzzleInterruptScenesUnseenOnly { get; set; }
    [Reactive]
    public bool PuzzleInterruptScenesOn { get; set; }
    [Reactive]
    public bool TopicStockMode { get; set; }
    [Reactive]
    public bool DialogueSkipping { get; set; }
    [Reactive]
    public bool BatchDialogueDisplay { get; set; }

    // Save Slot Data
    [Reactive]
    public DateTime SaveDate { get; set; }
    [Reactive]
    public TimeSpan SaveTime { get; set; }
    public ScenarioCommandPickerViewModel ScenarioCommandPickerVm { get; set; }
    [Reactive]
    public short Episode { get; set; }

    [Reactive] public byte HaruhiFriendshipLevel { get; set; }
    [Reactive] public byte AsahinaFriendshipLevel { get; set; }
    [Reactive] public byte NagatoFriendshipLevel { get; set; }
    [Reactive] public byte KoizumiFriendshipLevel { get; set; }
    [Reactive] public byte TsuruyaFriendshipLevel { get; set; }
    [Reactive] public byte UnknownFriendshipLevel { get; set; }

    public List<RecentObjective> RecentObjectives { get; set; }
    public List<ReactivePowerStatus> PowerStatuses { get; set; }

    // Quick Save Data
    public ObservableCollection<ScriptItem> ScriptItems { get; }
    private OrderedDictionary<ScriptSection, List<ScriptItemCommand>> _currentCommandTree;
    private ScriptItem _selectedScriptItem;
    public ScriptItem SelectedScriptItem
    {
        get => _selectedScriptItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedScriptItem, value);
            _currentCommandTree = _selectedScriptItem.GetScriptCommandTree(_project, _log);
            _selectedScriptItem.CalculateGraphEdges(_currentCommandTree, _log);
            ScriptSections.Clear();
            ScriptSections.AddRange(_selectedScriptItem.Event.ScriptSections);
            SelectedScriptSection = ScriptSections.First();
        }
    }
    public ObservableCollection<ScriptSection> ScriptSections { get; }
    private ScriptSection _selectedScriptSection;
    public ScriptSection SelectedScriptSection
    {
        get => _selectedScriptSection;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedScriptSection, value);
            ScriptCommandIndex = 0;
        }
    }
    private int _scriptCommandIndex;
    public int ScriptCommandIndex
    {
        get => _scriptCommandIndex;
        set
        {
            if (SelectedScriptSection is null)
            {
                return;
            }
            this.RaiseAndSetIfChanged(ref _scriptCommandIndex, value);
            _scriptPreview = _selectedScriptItem.GetScriptPreview(_currentCommandTree,
                _currentCommandTree[SelectedScriptSection][_scriptCommandIndex], _project, _log);
            (ScriptPreview, ErrorImagePath) = ScriptItem.GeneratePreviewImage(_scriptPreview, _project);
        }
    }
    private ScriptPreview _scriptPreview;
    [Reactive]
    public SKBitmap ScriptPreview { get; set; }
    [Reactive]
    public string ErrorImagePath { get; set; }

    // Flag Data
    private LocalizedFlag[] _flags;
    private LocalizedFlag[] _filteredFlags;
    public ObservableCollection<LocalizedFlag> VisibleFlags { get; } = [];

    private int _flagPage;
    public int? FlagPage
    {
        get => _flagPage + 1;
        set
        {
            if (value <= 0)
            {
                return;
            }
            this.RaiseAndSetIfChanged(ref _flagPage, (value ?? 1) - 1);
            VisibleFlags.Clear();
            VisibleFlags.AddRange(_filteredFlags[(12 * _flagPage)..Math.Min(_filteredFlags.Length, 12 * (_flagPage + 1))]);
        }
    }

    [Reactive]
    public int NumPages { get; set; } = Flags.NUM_FLAGS / 12 + 1;

}

public class LocalizedFlag(int id, Project project, bool isSet)
{
    public int Id { get; } = id;
    public string Description { get; } = Flags.GetFlagNickname(id, project);
    public bool IsSet { get; set; } = isSet;
}

public class RecentObjective(string letter, CharacterMask objective) : ReactiveObject
{
    public string Letter { get; } = letter;
    [Reactive]
    public bool FirstSelection { get; set; } = objective.HasFlag(CharacterMask.SELECTION1);
    [Reactive]
    public bool SecondSelection { get; set; } = objective.HasFlag(CharacterMask.SELECTION2);
    [Reactive]
    public bool KyonPresent { get; set; } = objective.HasFlag(CharacterMask.KYON);
    [Reactive]
    public bool HaruhiPresent { get; set; } = objective.HasFlag(CharacterMask.HARUHI);
    [Reactive]
    public bool AsahinaPresent { get; set; } = objective.HasFlag(CharacterMask.MIKURU);
    [Reactive]
    public bool NagatoPresent { get; set; } = objective.HasFlag(CharacterMask.NAGATO);
    [Reactive]
    public bool KoizumiPresent { get; set; } = objective.HasFlag(CharacterMask.KOIZUMI);

    public CharacterMask GetCharacterMask()
    {
        CharacterMask mask = 0;
        mask |= KyonPresent ? CharacterMask.KYON : 0;
        mask |= HaruhiPresent ? CharacterMask.HARUHI : 0;
        mask |= AsahinaPresent ? CharacterMask.MIKURU : 0;
        mask |= NagatoPresent ? CharacterMask.NAGATO : 0;
        mask |= KoizumiPresent ? CharacterMask.KOIZUMI : 0;
        mask |= FirstSelection ? CharacterMask.SELECTION1 : 0;
        mask |= SecondSelection ? CharacterMask.SELECTION2 : 0;
        return mask;
    }
}

public class ReactivePowerStatus(string name, CharacterPowerStatus status) : ReactiveObject
{
    public string Name { get; } = name;
    [Reactive] public byte Level { get; set; } = status.Level;
    [Reactive] public byte RemainingUses { get; set; } = status.RemainingUses;
    [Reactive] public byte UsesSinceLevelUp { get; set; } = status.UsesSinceLevelUp;
    [Reactive] public byte UsesToLevelUp { get; set; } = status.UsesToLevelUp;

    public void SetStatus(CharacterPowerStatus status)
    {
        status.Level = Level;
        status.RemainingUses = RemainingUses;
        status.UsesSinceLevelUp = UsesSinceLevelUp;
        status.UsesToLevelUp = UsesToLevelUp;
    }
}
