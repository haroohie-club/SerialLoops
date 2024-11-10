using System;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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

    public ObservableCollection<string> SfxPlayModes { get; } = new(Enum.GetNames<SfxModeScriptParameter.SfxMode>());
    private SfxModeScriptParameter.SfxMode _sfxMode;
    public string SfxMode
    {
        get => _sfxMode.ToString();
        set
        {
            this.RaiseAndSetIfChanged(ref _sfxMode, Enum.Parse<SfxModeScriptParameter.SfxMode>(value));
            ((SfxModeScriptParameter)Command.Parameters[1]).Mode = _sfxMode;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)_sfxMode;
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

    public SndPlayScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, MainWindowViewModel window) :
        base(command, scriptEditor)
    {
        Tabs = window.EditorTabs;
        SfxChoices = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.SFX && ((SfxItem)i).AssociatedGroups.Contains(window.OpenProject.Snd.Groups[Script.SfxGroupIndex].Name)).Cast<SfxItem>());
        _selectedSfx = ((SfxScriptParameter)Command.Parameters[0]).Sfx;
        _sfxMode = ((SfxModeScriptParameter)Command.Parameters[1]).Mode;
        _volume = ((ShortScriptParameter)Command.Parameters[2]).Value;
        _crossfadeTime = ((ShortScriptParameter)Command.Parameters[4]).Value;
        _loadSound = ((BoolScriptParameter)Command.Parameters[3]).Value && _crossfadeTime < 0;
    }
}