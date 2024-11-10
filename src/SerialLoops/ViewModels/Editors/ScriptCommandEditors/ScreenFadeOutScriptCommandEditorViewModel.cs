using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Controls;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ScreenFadeOutScriptCommandEditorViewModel : ScriptCommandEditorViewModel
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

    private SKColor _customColor;
    public SKColor CustomColor
    {
        get => _customColor;
        set
        {
            this.RaiseAndSetIfChanged(ref _customColor, value);
            ((ColorScriptParameter)Command.Parameters[2]).Color = _customColor;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = _customColor.Red;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = _customColor.Green;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[4] = _customColor.Blue;
            Script.UnsavedChanges = true;
        }
    }

    [Reactive]
    public ScreenSelectorViewModel ScreenSelector { get; set; }

    public ObservableCollection<string> Colors { get; } = new(Enum.GetNames<ColorMonochromeScriptParameter.ColorMonochrome>());
    private ColorMonochromeScriptParameter.ColorMonochrome _color;
    public string Color
    {
        get => _color.ToString();
        set
        {
            this.RaiseAndSetIfChanged(ref _color, Enum.Parse<ColorMonochromeScriptParameter.ColorMonochrome>(value));
            ((ColorMonochromeScriptParameter)Command.Parameters[4]).ColorType = _color;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[6] = (short)_color;
            Script.UnsavedChanges = true;
        }
    }

    public ScreenFadeOutScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor) : base(command, scriptEditor)
    {
        _fadeTime = ((ShortScriptParameter)Command.Parameters[0]).Value;
        _fadePercentage = ((ShortScriptParameter)Command.Parameters[1]).Value;
        _customColor = ((ColorScriptParameter)Command.Parameters[2]).Color;
        ScreenSelector = new(((ScreenScriptParameter)Command.Parameters[3]).Screen, true);
        ScreenSelector.ScreenChanged += (sender, args) =>
        {
            ((ScreenScriptParameter)Command.Parameters[3]).Screen = ScreenSelector.SelectedScreen;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[5] = (short)ScreenSelector.SelectedScreen;
            Script.UnsavedChanges = true;
        };
        _color = ((ColorMonochromeScriptParameter)Command.Parameters[4]).ColorType;
    }
}