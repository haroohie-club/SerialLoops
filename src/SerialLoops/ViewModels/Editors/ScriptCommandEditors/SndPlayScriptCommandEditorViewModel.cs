using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class SndPlayScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    [Reactive]
    public ObservableCollection<SfxItem> SfxChoices { get; set; }
    private SfxItem _selectedSfx;
    public SfxItem SelectedSfx
    {
        get => _selectedSfx;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSfx, value);
            ((SfxScriptParameter)Command.Parameters[0]).Sfx = _selectedSfx;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _selectedSfx.Index;
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<SfxModeLocalized> SfxPlayModes { get; } =
        new(Enum.GetValues<SfxModeScriptParameter.SfxMode>().Select(m => new SfxModeLocalized(m)));
    private SfxModeLocalized _sfxMode;
    public SfxModeLocalized SfxMode
    {
        get => _sfxMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _sfxMode, value);
            ((SfxModeScriptParameter)Command.Parameters[1]).Mode = _sfxMode.Mode;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)_sfxMode.Mode;
            Script.UnsavedChanges = true;
        }
    }

    private short _volume;
    public short Volume
    {
        get => _volume;
        set
        {
            this.RaiseAndSetIfChanged(ref _volume, value);
            ((ShortScriptParameter)Command.Parameters[2]).Value = _volume;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = _volume;
            Script.UnsavedChanges = true;
        }
    }

    private bool _loadSound;
    public bool LoadSound
    {
        get => _loadSound;
        set
        {
            this.RaiseAndSetIfChanged(ref _loadSound, value);
            if (_loadSound)
            {
                ((BoolScriptParameter)Command.Parameters[3]).Value = true;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[3] = ((BoolScriptParameter)Command.Parameters[3]).TrueValue;
                CrossfadeMin = -1;
                CrossfadeMax = -1;
                CrossfadeTime = -1;
            }
            else
            {
                CrossfadeMin = 0;
                CrossfadeMax = short.MaxValue;
                CrossfadeTime = 0;
            }
            Script.UnsavedChanges = true;
        }
    }

    [Reactive]
    public short CrossfadeMin { get; set; } = 0;

    [Reactive]
    public short CrossfadeMax { get; set; } = short.MaxValue;

    private short _crossfadeTime;
    public short CrossfadeTime
    {
        get => _crossfadeTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _crossfadeTime, value);
            ((ShortScriptParameter)Command.Parameters[4]).Value = _crossfadeTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[4] = _crossfadeTime;
            Script.UnsavedChanges = true;
        }
    }

    public SndPlayScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) :
        base(command, scriptEditor, log)
    {
        Tabs = window.EditorTabs;
        SfxChoices = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.SFX && ((SfxItem)i).AssociatedGroups.Contains(window.OpenProject.Snd.Groups[Script.SfxGroupIndex].Name)).Cast<SfxItem>());
        _selectedSfx = ((SfxScriptParameter)Command.Parameters[0]).Sfx;
        _sfxMode = new(((SfxModeScriptParameter)Command.Parameters[1]).Mode);
        _volume = ((ShortScriptParameter)Command.Parameters[2]).Value;
        _crossfadeTime = ((ShortScriptParameter)Command.Parameters[4]).Value;
        _loadSound = ((BoolScriptParameter)Command.Parameters[3]).Value && _crossfadeTime < 0;
    }
}

public readonly struct SfxModeLocalized(SfxModeScriptParameter.SfxMode mode)
{
    public SfxModeScriptParameter.SfxMode Mode { get; } = mode;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(mode.ToString());
}
