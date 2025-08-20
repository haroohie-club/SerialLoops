using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class HaruhiMeterScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, bool noShow)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public string[] Modes => NoShow ? [Strings.AddAmountLabel] : [Strings.AddAmountLabel, Strings.SetAmountLabel];
    private string _selectedMode = noShow || ((ShortScriptParameter)command.Parameters[1]).Value == 0 ? Strings.AddAmountLabel : Strings.SetAmountLabel;
    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMode, value);
            IsAdd = value == Strings.AddAmountLabel;
        }
    }
    [Reactive] public bool IsAdd { get; set; } = noShow || ((ShortScriptParameter)command.Parameters[1]).Value == 0;

    public bool NoShow { get; } = noShow;

    private short _addAmount = noShow ? (short)0 : ((ShortScriptParameter)command.Parameters[0]).Value;
    public short AddAmount
    {
        get => _addAmount;
        set
        {
            this.RaiseAndSetIfChanged(ref _addAmount, value);
            ((ShortScriptParameter)Command.Parameters[0]).Value = _addAmount;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = _addAmount;
        }
    }

    private short _setAmount = noShow ? (short)0 : ((ShortScriptParameter)command.Parameters[1]).Value;
    public short SetAmount
    {
        get => _setAmount;
        set
        {
            this.RaiseAndSetIfChanged(ref _setAmount, value);
            ((ShortScriptParameter)Command.Parameters[1]).Value = _setAmount;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = _setAmount;
        }
    }

    private short _addNoShowAmount = !noShow ? ((ShortScriptParameter)command.Parameters[0]).Value : (short)0;
    public short AddNoShowAmount
    {
        get => _addNoShowAmount;
        set
        {
            this.RaiseAndSetIfChanged(ref _addNoShowAmount, value);
            ((ShortScriptParameter)Command.Parameters[0]).Value = _addNoShowAmount;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _addNoShowAmount;
        }
    }
}
