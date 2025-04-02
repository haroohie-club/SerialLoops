using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class PalEffectScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public ObservableCollection<PaletteEffectLocalized> PaletteEffects { get; } =
       new(Enum.GetValues<PaletteEffectScriptParameter.PaletteEffect>().Select(e => new PaletteEffectLocalized(e)));
    private PaletteEffectLocalized _paletteEffect = new(((PaletteEffectScriptParameter)command.Parameters[0]).Effect);
    public PaletteEffectLocalized PaletteEffect
    {
        get => _paletteEffect;
        set
        {
            this.RaiseAndSetIfChanged(ref _paletteEffect, value);
            ((PaletteEffectScriptParameter)Command.Parameters[0]).Effect = _paletteEffect.Effect;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_paletteEffect.Effect;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    private short _transitionTime = ((ShortScriptParameter)command.Parameters[1]).Value;
    public short TransitionTime
    {
        get => _transitionTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _transitionTime, value);
            ((ShortScriptParameter)Command.Parameters[1]).Value = _transitionTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = _transitionTime;
            Script.UnsavedChanges = true;
        }
    }

    private bool _unknown = ((BoolScriptParameter)command.Parameters[2]).Value;
    public bool Unknown
    {
        get => _unknown;
        set
        {
            this.RaiseAndSetIfChanged(ref _unknown, value);
            ((BoolScriptParameter)Command.Parameters[2]).Value = _unknown;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _unknown ? ((BoolScriptParameter)Command.Parameters[2]).TrueValue : ((BoolScriptParameter)Command.Parameters[2]).FalseValue;
        }
    }
}

public readonly struct PaletteEffectLocalized(PaletteEffectScriptParameter.PaletteEffect effect)
{
    public PaletteEffectScriptParameter.PaletteEffect Effect { get; } = effect;
    public override string ToString() => Strings.ResourceManager.GetString(Effect.ToString());
}
