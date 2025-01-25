using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class InvestStartScriptCommandEditorViewModel(
    ScriptItemCommand scriptCommand,
    ScriptEditorViewModel scriptEditor,
    ILogger log)
    : ScriptCommandEditorViewModel(scriptCommand, scriptEditor, log)
{
    public short MapCharacterSet
    {
        get => ((ShortScriptParameter)Command.Parameters[0]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[0]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = value;
            this.RaisePropertyChanged();
            Script.UnsavedChanges = true;
        }
    }
    public short Unknown1
    {
        get => ((ShortScriptParameter)Command.Parameters[1]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[1]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = value;
            this.RaisePropertyChanged();
            Script.UnsavedChanges = true;
        }
    }

    public short Unknown2
    {
        get => ((ShortScriptParameter)Command.Parameters[2]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[2]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = value;
            this.RaisePropertyChanged();
            Script.UnsavedChanges = true;
        }
    }

    public short Unknown3
    {
        get => ((ShortScriptParameter)Command.Parameters[3]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[3]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = value;
            this.RaisePropertyChanged();
            Script.UnsavedChanges = true;
        }
    }

    public ReactiveScriptSection EndScriptSection
    {
        get => ScriptEditor.ScriptSections.First(s =>
            s.Section.Name.Equals(((ScriptSectionScriptParameter)Command.Parameters[4]).Section.Name));
        set
        {
            ((ScriptSectionScriptParameter)Command.Parameters[4]).Section = value.Section;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[4] =
                Script.Event.LabelsSection.Objects.First(l => l.Name.Replace("/", "").Equals(value.Name)).Id;
            this.RaisePropertyChanged();
            Script.UnsavedChanges = true;
        }
    }
}
