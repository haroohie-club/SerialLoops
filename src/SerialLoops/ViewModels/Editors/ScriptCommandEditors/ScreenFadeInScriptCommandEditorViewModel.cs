using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Controls;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ScreenFadeInScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private short _fadeTime;
    public short FadeTime
    {
        get => _fadeTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _fadeTime, value);
            ((ShortScriptParameter)Command.Parameters[0]).Value = _fadeTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _fadeTime;
            Script.UnsavedChanges = true;
        }
    }

    private short _fadePercentage;
    public short FadePercentage
    {
        get => _fadePercentage;
        set
        {
            this.RaiseAndSetIfChanged(ref _fadePercentage, value);
            ((ShortScriptParameter)Command.Parameters[1]).Value = _fadePercentage;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = _fadePercentage;
            Script.UnsavedChanges = true;
        }
    }

    [Reactive]
    public ScreenSelectorViewModel ScreenSelector { get; set; }

    public ObservableCollection<ColorMonochromeLocalized> Colors { get; } =
        new(Enum.GetValues<ColorMonochromeScriptParameter.ColorMonochrome>().Select(c => new ColorMonochromeLocalized(c)));
    private ColorMonochromeLocalized _color;
    public ColorMonochromeLocalized Color
    {
        get => _color;
        set
        {
            this.RaiseAndSetIfChanged(ref _color, value);
            ((ColorMonochromeScriptParameter)Command.Parameters[3]).ColorType = _color.Color;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = (short)_color.Color;
            Script.UnsavedChanges = true;
        }
    }

    public ScreenFadeInScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : base(command, scriptEditor, log)
    {
        _fadeTime = ((ShortScriptParameter)command.Parameters[0]).Value;
        _fadePercentage = ((ShortScriptParameter)command.Parameters[1]).Value;
        ScreenSelector = new(((ScreenScriptParameter)command.Parameters[2]).Screen, true);
        ScreenSelector.ScreenChanged += (sender, args) =>
        {
            ((ScreenScriptParameter)Command.Parameters[2]).Screen = ScreenSelector.SelectedScreen;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = (short)ScreenSelector.SelectedScreen;
            Script.UnsavedChanges = true;
        };
        _color = new(((ColorMonochromeScriptParameter)command.Parameters[3]).ColorType);
    }
}

public readonly struct ColorMonochromeLocalized(ColorMonochromeScriptParameter.ColorMonochrome color)
{
    public ColorMonochromeScriptParameter.ColorMonochrome Color { get; } = color;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(color.ToString());
}
