using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class BgmPlayScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<BackgroundMusicItem> Bgms { get; }
    private BackgroundMusicItem _music;
    public BackgroundMusicItem Music
    {
        get => _music;
        set
        {
            this.RaiseAndSetIfChanged(ref _music, value);
            ((BgmScriptParameter)Command.Parameters[0]).Bgm = _music;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_music.Index;
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<BgmModeLocalized> Modes { get; } = new(Enum.GetValues<BgmModeScriptParameter.BgmMode>()
        .Select(m => new BgmModeLocalized(m)));
    private BgmModeLocalized _mode;
    public BgmModeLocalized Mode
    {
        get => _mode;
        set
        {
            this.RaiseAndSetIfChanged(ref _mode, value);
            ((BgmModeScriptParameter)Command.Parameters[1]).Mode = _mode.Mode;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)_mode.Mode;
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

    private short _fadeInTime;
    public short FadeInTime
    {
        get => _fadeInTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _fadeInTime, value);
            ((ShortScriptParameter)Command.Parameters[3]).Value = _fadeInTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = _fadeInTime;
            Script.UnsavedChanges = true;
        }
    }

    private short _fadeOutTime;
    public short FadeOutTime
    {
        get => _fadeOutTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _fadeOutTime, value);
            ((ShortScriptParameter)Command.Parameters[4]).Value = _fadeOutTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[4] = _fadeOutTime;
        }
    }

    public BgmPlayScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window)
        : base(command, scriptEditor, log)
    {
        Tabs = window.EditorTabs;
        Bgms = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.BGM)
            .Cast<BackgroundMusicItem>());
        _music = ((BgmScriptParameter)Command.Parameters[0]).Bgm;
        _mode = new(((BgmModeScriptParameter)Command.Parameters[1]).Mode);
        _volume = ((ShortScriptParameter)Command.Parameters[2]).Value;
        _fadeInTime = ((ShortScriptParameter)Command.Parameters[3]).Value;
        _fadeOutTime = ((ShortScriptParameter)Command.Parameters[4]).Value;
    }
}

public readonly struct BgmModeLocalized(BgmModeScriptParameter.BgmMode mode)
{
    public string DisplayString { get; } = Strings.ResourceManager.GetString(mode.ToString());
    public BgmModeScriptParameter.BgmMode Mode { get; } = mode;
}
